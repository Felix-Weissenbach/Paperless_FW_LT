using Microsoft.AspNetCore.Mvc;
using Paperless.DAL;
using Paperless.Logging;
using Paperless.Models;
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

        public DocumentController(PaperlessDbContext context)
        {
            _context = context;
            _logger = Logging.LoggerFactory.GetLogger();
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var documents = _context.Documents.ToList();
            return Ok(documents);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DocumentDTO doc)
        {
            if(doc == null)
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

                var message = $"New document uploaded: {newDoc.Id}";
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
