namespace Paperless.Models
{
    public class DocumentSearchResult
    {
        public string DocumentId { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

}
