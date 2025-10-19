using System.Diagnostics;

namespace Paperless.Logging
{
    public class LoggerFactory
    {
        public static ILoggerWrapper GetLogger()
        {
            StackTrace stackTrace = new(1, false); //Captures 1 frame, false for not collecting information about the file
            var type = stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType;
            // the config file is copied to the output directory
            return Log4NetWrapper.CreateLogger("./Logging/log4net.config", type?.FullName ?? "");
        }
    }
}
