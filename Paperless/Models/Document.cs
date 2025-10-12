using System.ComponentModel.DataAnnotations;

namespace Paperless.Models
{
    public class Document
    {
        public Document()
        {
            CreatedAt = DateTime.UtcNow;
        }
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}