using Microsoft.AspNetCore.Mvc;
using Paperless.DAL;
using Paperless.ElasticSearch;
using Paperless.Logging;
using Paperless.Models;
using Paperless.Storage;
using RabbitMQ.Client;
using System.Text;

namespace Paperless.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly PaperlessDbContext _context;
        private readonly ILoggerWrapper _logger;
        private readonly IFileStorage _storage;

        public DocumentController(PaperlessDbContext context, IFileStorage storage, ILoggerWrapper logger)
        {
            _context = context;
            _storage = storage;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var documents = _context.Documents.ToList();
            return Ok(documents);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] DocumentDTO doc, [FromForm] IFormFile file)
        {
            if(file == null || file.Length == 0)
                return BadRequest("File is required.");
            if (doc == null)
                return BadRequest("Document data is required.");

            if (doc.Id == Guid.Empty)
                doc.Id = Guid.NewGuid();

            var newDoc = new Document(doc);

            try
            {
                _context.Documents.Add(newDoc);
                await _context.SaveChangesAsync();
                _logger.Info($"Document created with ID: {newDoc.Id}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Database error: {ex.Message}");
                return StatusCode(500, "Error saving document to the database.");
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var objectName = newDoc.Id + ".pdf";

                await _storage.UploadFileAsync("documents", objectName, stream);

                _logger.Info($"Uploaded PDF to MinIO: {objectName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"MinIO upload failed: {ex.Message}");
                return StatusCode(500, "Error uploading file to MinIO.");
            }

            // --- Async RabbitMQ section ---
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = "rabbitmq", // docker-compose service name
                    UserName = "user",
                    Password = "password"
                };

                await using var connection = await factory.CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();

                _logger.Info("Connected to RabbitMQ.");

                await channel.QueueDeclareAsync(
                    queue: "ocr_queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var message = newDoc.Id.ToString() + ".pdf";
                var body = Encoding.UTF8.GetBytes(message);

                var props = new BasicProperties { 
                    ContentType = "text/plain",
                    DeliveryMode = DeliveryModes.Transient
                };


                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "ocr_queue",
                    mandatory: false,
                    basicProperties: props,
                    body: body,
                    cancellationToken: CancellationToken.None
                );

                _logger.Info($"Published message to queue: {message}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to publish message to RabbitMQ: {ex.Message}");
                return StatusCode(500, "Error publishing message to RabbitMQ.");
            }

            return CreatedAtAction(nameof(GetAll), new { id = newDoc.Id }, newDoc);
        }

        [HttpPost("{id}/summary")]
        public async Task<IActionResult> StoreSummary(Guid id, [FromBody] SummaryDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Summary))
                return BadRequest("Summary is required.");

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null)
                return NotFound($"Document with ID {id} not found.");

            doc.Summary = dto.Summary;

            try
            {
                await _context.SaveChangesAsync();
                _logger.Info($"Summary stored for document {id}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Database error while storing summary: {ex.Message}");
                return StatusCode(500, "Could not save summary to database.");
            }

            return Ok();
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            _logger.Info($"Received search request with query: {query}");
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query parameter is required.");

            var client = ElasticClientFactory.Create();

            var response = await client.SearchAsync<DocumentSearchResult>(s => s
                .Indices("documents")
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Content)
                        .Query(query)
                    )
                )
            );

            if (!response.IsValidResponse)
            {
                _logger.Error($"Elasticsearch search failed: {response.DebugInformation}");
                return StatusCode(500, "Elasticsearch search failed.");
            }

            var results = response.Hits
                .Where(h => h.Source != null)
                .Select(h => new
                {
                    Id = h.Source!.DocumentId,
                    FileName = h.Source.FileName
                })
                .ToList();

            _logger.Info($"Search completed. Found {results.Count} results.");

            return new JsonResult(results)
            { 
                StatusCode = 200,
                ContentType = "application/json"
            };
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null)
                return NotFound();

            var fileStream = await _storage.GetFileAsync("documents", doc.Id + ".pdf");
            if (fileStream == null)
                return NotFound();

            return File(fileStream, "application/pdf", doc.FileName);
        }


        public class SummaryDto
        {
            public string Summary { get; set; } = "";
        }
    }
}
