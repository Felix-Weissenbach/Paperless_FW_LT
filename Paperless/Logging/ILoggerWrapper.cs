namespace Paperless.Logging
{
    public interface ILoggerWrapper
    {
        void Debug(string message);
        void Error(string message);
        void Fatal(string message);
        void Warn(string message);
        void Info(string message);
    }
}
