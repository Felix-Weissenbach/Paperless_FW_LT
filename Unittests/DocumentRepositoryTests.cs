using Paperless.DAL;
using Paperless.Models;
using NSubstitute;
using Microsoft.EntityFrameworkCore;

namespace Unittests
{
    internal class DocumentRepositoryTests
    {
        [Test]
        public async Task AddAsyncShouldAddDocument()
        {
            var options = new DbContextOptionsBuilder<PaperlessDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            using var context = new PaperlessDbContext(options);
            var repo = new DocumentRepository(context);

            await repo.AddAsync(new Document());

            Assert.That(await context.Documents.CountAsync(), Is.EqualTo(1));
        }
    }
}
