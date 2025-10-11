namespace Paperless.Models
{
    public class Document
    {
        public Document()
        {
            CreatedAt = DateTime.UtcNow;
        }
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}