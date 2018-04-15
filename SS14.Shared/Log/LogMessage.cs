using System;

namespace SS14.Shared.Log
{
    public struct LogMessage
    {
        public readonly string Message;
        public readonly LogLevel Level;
        public readonly string SawmillName;

        public LogMessage(string message, LogLevel level, string sawmillName)
        {
            Message = message;
            Level = level;
            SawmillName = sawmillName;
        }

        public static ConsoleColor LogLevelToConsoleColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return ConsoleColor.DarkBlue;

                case LogLevel.Info:
                    return ConsoleColor.Cyan;

                case LogLevel.Warning:
                    return ConsoleColor.Yellow;

                case LogLevel.Error:
                case LogLevel.Fatal:
                    return ConsoleColor.Red;

                default:
                    return ConsoleColor.White;
            }
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
