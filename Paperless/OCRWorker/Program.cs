using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Minio;
using Minio.DataModel.Args;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


namespace OcrWorker
{
    public class Program
    {
        public static class MinioCredentials
        {
            public readonly static string Endpoint;
            public readonly static string AccessKey;
            public readonly static string SecretKey;
            static MinioCredentials()
            {
                Endpoint = Environment.GetEnvironmentVariable("Minio__Endpoint") ?? "minio:9000";
                AccessKey = Environment.GetEnvironmentVariable("Minio__AccessKey") ?? "minio";
                SecretKey = Environment.GetEnvironmentVariable("Minio__SecretKey") ?? "minio123";
            }
        }
        public static async Task Main(string[] args)
        {
            Console.WriteLine("OCR Worker starting...");

            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RabbitMQ__Host") ?? "rabbitmq",
                UserName = Environment.GetEnvironmentVariable("RabbitMQ__Username") ?? "user",
                Password = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "password"
            };

            var queueName = Environment.GetEnvironmentVariable("RabbitMQ__Queue") ?? "ocr_queue";

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var genaiQueue = Environment.GetEnvironmentVariable("RabbitMQ__GenAIQueue") ?? "genai_queue";

            await channel.QueueDeclareAsync(
                queue: genaiQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );


            Console.WriteLine($"Listening for messages on queue: {queueName}");

            await EnsureBucketExists("ocr-results");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var pdfName = Encoding.UTF8.GetString(body);
                var documentId = Path.GetFileNameWithoutExtension(pdfName);

                var pdfPath = await DownloadPdfFromMinio(pdfName);
                var images = await ConvertPdfToImages(pdfPath);
                var ocrText = await PerformOcrOnImages(images);
                await UploadOcrResultToMinio(Path.GetFileNameWithoutExtension(pdfName), ocrText);

                Console.WriteLine($"Processed OCR for {pdfName}");

                // --- SEND OCR RESULT TO GENAI WORKER ---
                var genaiPayload = new
                {
                    DocumentId = documentId,
                    OcrText = ocrText
                };

                var genaiJson = System.Text.Json.JsonSerializer.Serialize(genaiPayload);
                var genaiBody = Encoding.UTF8.GetBytes(genaiJson);

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: genaiQueue,
                    body: genaiBody
                );

                Console.WriteLine($"Sent OCR text to GenAI queue {genaiQueue}");

                // --- PUBLISH OCR RESULT TO ELASTICSEARCH INDEXING WORKER ---
                var payload = new
                {
                    DocumentId = documentId,
                    FileName = pdfName,
                    OcrText = ocrText
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "index_queue", // should maybe use an environment variable here too, oh well, maybe later
                    body: Encoding.UTF8.GetBytes(json)
                );

                Console.WriteLine($"Published message to indexing queue for document {documentId}");

            };

            await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

            // keep process alive indefinitely
            await Task.Delay(Timeout.Infinite);
        }

        public static async Task<string> DownloadPdfFromMinio(string objectName)
        {
            var minio = new MinioClient()
                .WithEndpoint(MinioCredentials.Endpoint)
                .WithCredentials(MinioCredentials.AccessKey, MinioCredentials.SecretKey)
                .Build();

            var localPath = "/tmp/" + objectName;

            var args = new GetObjectArgs()
                .WithBucket("documents")
                .WithObject(objectName)
                .WithFile(localPath);

            await minio.GetObjectAsync(args);

            Console.WriteLine($"Downloaded {objectName} to {localPath}");
            return localPath;
        }

        public static async Task<List<string>> ConvertPdfToImages(string pdfPath)
        {
            var outputBase = "/tmp/page";
            var args = $"-dNOPAUSE -dBATCH -sDEVICE=pngalpha -r300 -sOutputFile={outputBase}%03d.png {pdfPath}";

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "gs",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            var images = Directory.GetFiles("/tmp", "page*.png").ToList();
            Console.WriteLine($"Converted PDF to {images.Count} images.");

            return images;
        }

        public static async Task<string> PerformOcrOnImages(List<string> imagePaths)
        {
            var resultBuilder = new StringBuilder();

            foreach (var image in imagePaths)
            {
                var outputTxt = image + ".txt";
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "tesseract",
                        Arguments = $"{image} {image}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                resultBuilder.AppendLine(await File.ReadAllTextAsync(outputTxt));
            }

            Console.WriteLine("Completed OCR on images.");

            return resultBuilder.ToString();
        }

        public static async Task UploadOcrResultToMinio(string objectName, string ocrText)
        {
            Console.WriteLine("Uploading OCR result to MinIO...");
            var minio = new MinioClient()
                .WithEndpoint(MinioCredentials.Endpoint)
                .WithCredentials(MinioCredentials.AccessKey, MinioCredentials.SecretKey)
                .Build();

            var tempFilePath = "/tmp/" + objectName + ".txt";

            await File.WriteAllTextAsync(tempFilePath, ocrText);
            Console.WriteLine("OCR result written to temporary file.");

            var args = new PutObjectArgs()
                .WithBucket("ocr-results")
                .WithObject(objectName + ".txt")
                .WithFileName(tempFilePath)
                .WithContentType("text/plain");

            await minio.PutObjectAsync(args);

            Console.WriteLine($"Uploaded OCR result to MinIO: {objectName}.txt");
        }

        public static async Task EnsureBucketExists(string bucketName)
        {
            var minio = new MinioClient()
                .WithEndpoint(MinioCredentials.Endpoint)
                .WithCredentials(MinioCredentials.AccessKey, MinioCredentials.SecretKey)
                .Build();

            bool found = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!found)
            {
                await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                Console.WriteLine($"Created bucket: {bucketName}");
            }
        }
    }
}