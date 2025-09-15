namespace Paperless.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public List<string> Authors { get; set; }

        public Document(int id, string title, string author)
        {
            Id = id;
            Title = title;
            Authors = new List<string> { author };
        }
        public Document(int id, string title, List<string> authors)
        {
            Id = id;
            Title = title;
            Authors = authors;
        }
    }
}
