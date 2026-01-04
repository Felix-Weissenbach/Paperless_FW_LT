using Npgsql;
using System.Xml.Linq;

namespace Paperless.AccessLogBatch
{
    public class Program
    {
        static async Task Main(string[] args)
        {

            Console.WriteLine("AccessLog batch job started");

            var inputDir = Environment.GetEnvironmentVariable("InputFolder") ?? "/input";
            var archiveDir = Environment.GetEnvironmentVariable("ArchiveFolder") ?? "/archive";
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.WriteLine("No database connection string configured.");
                return;
            }

            Directory.CreateDirectory(archiveDir);

            foreach (var file in Directory.GetFiles(inputDir, "*.xml"))
            {
                Console.WriteLine($"Processing {file}");

                try
                {
                    var doc = XDocument.Load(file);

                    using var conn = new NpgsqlConnection(connectionString);
                    await conn.OpenAsync();

                    foreach (var entry in doc.Root!.Elements("document"))
                    {
                        var documentId = Guid.Parse(entry.Element("documentId")!.Value);
                        var accessCount = int.Parse(entry.Element("accessCount")!.Value);

                        var cmd = new NpgsqlCommand(
                            """
                            UPDATE documents
                            SET "DailyAccessCount" = "DailyAccessCount" + @count
                            WHERE "Id" = @id
                            """,
                            conn
                        );

                        cmd.Parameters.AddWithValue("id", documentId);
                        cmd.Parameters.AddWithValue("count", accessCount);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    var archivedPath = Path.Combine(archiveDir, Path.GetFileName(file));
                    File.Move(file, archivedPath, overwrite: true);

                    Console.WriteLine($"Archived {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process {file}: {ex.Message}");
                }
            }

            Console.WriteLine("AccessLog batch job finished");

        }
    }
}
