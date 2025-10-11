namespace Paperless.Models
{
    public class DocumentDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        public DocumentDTO(string title, string content)
        {
            Id = new Guid();
            Title = title;
            Content = content;
        }
        public DocumentDTO(Guid id, string title, string content)
        {
            Id = id;
            Title = title;
            Content = content;
        }

        public DocumentDTO() : this(string.Empty, string.Empty) { }
    }
}
