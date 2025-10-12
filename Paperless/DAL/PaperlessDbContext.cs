using Microsoft.EntityFrameworkCore;
using Paperless.Models;

namespace Paperless.DAL
{
    public class PaperlessDbContext : DbContext
    {
        public PaperlessDbContext(DbContextOptions<PaperlessDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) 
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (entity.GetTableName() != null)
                    entity.SetTableName(entity.GetTableName()!.ToLower());
            }

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Document> Documents { get; set; }
    }
}