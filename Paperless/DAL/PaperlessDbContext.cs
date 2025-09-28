using Microsoft.EntityFrameworkCore;
using Paperless.Models;

namespace Paperless.DAL
{
    public class PaperlessDbContext : DbContext
    {
        public PaperlessDbContext(DbContextOptions<PaperlessDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; }
    }
}