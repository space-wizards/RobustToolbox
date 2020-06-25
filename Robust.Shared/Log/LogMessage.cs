namespace Robust.Shared.Log
{
    public readonly struct LogMessage
    {
        public const string LogNameDebug = "DEBG";
        public const string LogNameInfo = "INFO";
        public const string LogNameWarning = "WARN";
        public const string LogNameError = "ERRO";
        public const string LogNameFatal = "FATL";
        public const string LogNameUnknown = "UNKO";

        /// <summary>
        ///     The actual log message given.
        /// </summary>
        public readonly string Message;

        /// <summary>
        ///     The log level of the message.
        /// </summary>
        public readonly LogLevel Level;

        /// <summary>
        ///     The name of the sawmill that sent the message.
        /// </summary>
        public readonly string SawmillName;

        public LogMessage(string message, LogLevel level, string sawmillName)
        {
            Message = message;
            Level = level;
            SawmillName = sawmillName;
        }

        public string LogLevelToName()
        {
            return LogLevelToName(Level);
        }

        public static string LogLevelToName(LogLevel level)
        {
            return level switch
            {
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
