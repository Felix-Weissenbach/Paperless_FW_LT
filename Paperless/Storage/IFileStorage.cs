namespace Paperless.Storage
{
    public interface IFileStorage
    {
        Task UploadFileAsync(string bucket, string objectName, Stream data);
        Task<Stream> GetFileAsync(string bucket, string objectName);
    }
}
