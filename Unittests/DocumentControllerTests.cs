using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Paperless.Controllers;
using Paperless.DAL;
using Paperless.Logging;
using Paperless.Models;
using Paperless.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unittests
{
    internal class DocumentControllerTests
    {
        private readonly ILoggerWrapper mockLogger = Substitute.For<ILoggerWrapper>();
        private readonly IFileStorage mockStorage = Substitute.For<IFileStorage>();
        private readonly PaperlessDbContext mockDbContext = Substitute.For<PaperlessDbContext>(new DbContextOptions<PaperlessDbContext>());
        private readonly DocumentController controller;

        public DocumentControllerTests()
        {
            controller = new DocumentController(mockDbContext, mockStorage, mockLogger);
        }

        [Test]
        public void GetAllReturnsOkAndListOfDocuments()
        {
            // Arrange
            var documents = new List<Document>
            {
                new Document(),
                new Document()
            };
            mockDbContext.Documents.Returns(documents.AsQueryable());
            // Act
            var result = controller.GetAll();
            // Assert
            Assert.That(result.ToString(), Is.EqualTo("")); // fix this
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            // Any necessary cleanup after all tests
            mockDbContext.Dispose();
        }
    }
}
