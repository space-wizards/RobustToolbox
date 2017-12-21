using SS14.Shared.Interfaces.Log;
using System;

namespace SS14.Shared.Log
{
    /// <summary>
    /// Generic logger. Dumps to <see cref="System.Console"/> and that's it.
    /// </summary>
    public class LogManager : ILogManager
    {
        #region ILogManager Members

        public LogLevel CurrentLevel { get; set; } = LogLevel.Debug;

        public void Log(string message, LogLevel level = LogLevel.Information, params object[] args)
        {
            if ((int)level < (int)CurrentLevel)
            {
                // Too low level, ignore it.
                return;
            }
            LogInternal(string.Format(message, args), level);
        }

        public void Debug(string message, params object[] args) => Log(message, LogLevel.Debug, args);
        public void Info(string message, params object[] args) => Log(message, LogLevel.Information, args);
        public void Warning(string message, params object[] args) => Log(message, LogLevel.Warning, args);
        public void Error(string message, params object[] args) => Log(message, LogLevel.Error, args);
        public void Fatal(string message, params object[] args) => Log(message, LogLevel.Fatal, args);

        #endregion ILogManager Members

        /// <summary>
        /// Actual method used to log the formatted message somewhere.
        /// To disk, console, etc...
        /// </summary>
        protected virtual void LogInternal(string message, LogLevel level)
        {
            string name = LogLevelToName(level);
            ConsoleColor color = LogLevelToConsoleColor(level);

            System.Console.ForegroundColor = color;
            System.Console.Write(name);
            System.Console.ResetColor();
            System.Console.WriteLine(": {0}", message);
        }

        // If you make either of these methods public.
        // Put them somewhere else.
        // This would be a terrible spot.
        protected static ConsoleColor LogLevelToConsoleColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return ConsoleColor.DarkBlue;

                case LogLevel.Information:
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

        protected static string LogLevelToName(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return "DEBG";

                case LogLevel.Information:
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
