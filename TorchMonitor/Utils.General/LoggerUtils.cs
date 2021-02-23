using NLog;

namespace Utils.General
{
    internal static class LoggerUtils
    {
        public static ILogger GetFullNameLogger(this object self)
        {
            return LogManager.GetLogger(self.GetType().FullName);
        }
    }
}