using Microsoft.EntityFrameworkCore;
using Paperless.Models;

namespace Paperless.DAL
{
    public class DocumentRepository : IDocumentRepository //Note: To check repo use: docker exec -it paperless-db psql -U postgres -d paperless
    {
        private readonly PaperlessDbContext _context;
        public DocumentRepository(PaperlessDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Document>> GetAllAsync()
        {
            return await _context.Documents.ToListAsync();
        }
        public async Task AddAsync(Document document)
        {
            await _context.Documents.AddAsync(document);
            await _context.SaveChangesAsync();
        }
    }
}
