namespace SS14.Shared.Log
{
    public struct LogMessage
    {
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
            switch (level)
            {
                case LogLevel.Debug:
                    return "DEBG";

                case LogLevel.Info:
                    return "INFO";

                case LogLevel.Warning:
                    return "WARN";

                case LogLevel.Error:
                    return "ERRO";

                case LogLevel.Fatal:
                    return "FATL";

                default:
                    return "UNKO";
            }
        }
    }
}
