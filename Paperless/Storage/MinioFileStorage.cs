using Minio;
using Minio.DataModel.Args;
using Paperless.Logging;

namespace Paperless.Storage
{
    public class MinioFileStorage : IFileStorage
    {
        private readonly IMinioClient _client;
        private readonly string _bucket;
        private readonly ILoggerWrapper _logger;

        public MinioFileStorage(IConfiguration config, ILoggerWrapper logger)
        {
            _logger = logger;
            var endpoint = config["Minio:Endpoint"] ?? throw new Exception("Minio:Endpoint not configured!");
            var accessKey = config["Minio:AccessKey"] ?? throw new Exception("Minio:AccessKey not configured!");
            var secretKey = config["Minio:SecretKey"] ?? throw new Exception("Minio:SecretKey not configured!");

            _bucket = config["Minio:Bucket"] ?? "documents";

            _client = new MinioClient()
                                .WithEndpoint(endpoint)
                                .WithCredentials(accessKey, secretKey)
                                .WithSSL(false)
                                .Build();
        }

        public async Task UploadFileAsync(string bucketName, string objectName, Stream dataStream)
        {
            try
            {
                bool found = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
                if (!found)
                {
                    _logger.Info($"Bucket '{bucketName}' does not exist. Creating it.");
                    await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                }
                await _client.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(dataStream)
                    .WithObjectSize(dataStream.Length)
                    .WithContentType("application/pdf"));
                _logger.Info($"File '{objectName}' uploaded to bucket '{bucketName}'.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error uploading file '{objectName}' to bucket '{bucketName}': {ex.Message}");
                throw;
            }
        }

        public async Task<Stream> GetFileAsync(string bucketName, string objectName)
        {
            try
            {
                MemoryStream memoryStream = new MemoryStream();
                await _client.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithCallbackStream(stream =>
                    {
                        stream.CopyTo(memoryStream);
                    }));
                memoryStream.Position = 0; // Reset stream position
                _logger.Info($"File '{objectName}' retrieved from bucket '{bucketName}'.");
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error retrieving file '{objectName}' from bucket '{bucketName}': {ex.Message}");
                throw;
            }
        }
    }
}
