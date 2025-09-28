using Paperless.Models;

namespace Paperless.DAL
{
    public interface IDocumentRepository
    {
        Task<IEnumerable<Document>> GetAllAsync();
        Task AddAsync(Document document);
    }
}
