using Microsoft.AspNetCore.Mvc;
using Paperless.DAL;
using Paperless.Models;

namespace Paperless.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly PaperlessDbContext _context;

        public DocumentController(PaperlessDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var documents = _context.Documents.ToList();
            return Ok(documents);
        }

        [HttpPost]
        public IActionResult Create([FromBody] DocumentDTO doc)
        {
            if (doc.Id == Guid.Empty)
            {
                doc.Id = Guid.NewGuid();
            }
            Document newDoc = new Document
            {
                Id = doc.Id,
                Title = doc.Title,
                Content = doc.Content
            };

            _context.Documents.Add(newDoc);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetAll), new { id = doc.Id }, doc);
        }
    }
}
