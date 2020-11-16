namespace Robust.Shared.Log
{
    public static class LogMessage
    {
        public const string LogNameVerbose = "VERB";
        public const string LogNameDebug = "DEBG";
        public const string LogNameInfo = "INFO";
        public const string LogNameWarning = "WARN";
        public const string LogNameError = "ERRO";
        public const string LogNameFatal = "FATL";
        public const string LogNameUnknown = "UNKO";

        public static string LogLevelToName(LogLevel level)
        {
            return level switch
            {
                LogLevel.Verbose => LogNameVerbose,
                LogLevel.Debug => LogNameDebug,
                LogLevel.Info => LogNameInfo,
                LogLevel.Warning => LogNameWarning,
                LogLevel.Error => LogNameError,
                LogLevel.Fatal => LogNameFatal,
                _ => LogNameUnknown
            };
        }
    }
}
