using Microsoft.AspNetCore.Mvc;
using Paperless.Models;

namespace Paperless.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(new[] { new DocumentDTO(new Guid(), "Sample Doc", "Sample Author" ) });
        }

        [HttpPost]
        public IActionResult Create([FromBody] DocumentDTO doc)
        {
            return CreatedAtAction(nameof(GetAll), new { id = doc.Id }, doc);
        }
    }
}
