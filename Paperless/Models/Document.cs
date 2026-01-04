using System.ComponentModel.DataAnnotations;

namespace Paperless.Models
{
    public class Document
    {
        public Document() { }
        public Document(DocumentDTO dto)
        {
            Id = dto.Id;
            FileName = dto.FileName;
            StoragePath = dto.StoragePath;
            FileSize = dto.FileSize;
            UploadedAt = dto.UploadedAt;
        }
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty;
        public string? StoragePath { get; set; } // might be useful in case of duplicate file names
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string? Summary { get; set; } // Generated summary from GenAI
        public int DailyAccessCount { get; set; } = 0; // Count of accesses in the last day
    }
}