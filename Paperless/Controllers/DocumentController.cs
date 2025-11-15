using Microsoft.AspNetCore.Mvc;
using Paperless.DAL;
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
                    durable: false,
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
    }
}
