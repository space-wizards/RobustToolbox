using SS14.Shared.Interfaces.Log;
using System;

namespace SS14.Shared.Log
{
    /// <summary>
    ///     Log handler that prints to console.
    /// </summary>
    public sealed class ConsoleLogHandler : ILogHandler
    {
        public void Log(LogMessage message)
        {
            string name = LogMessage.LogLevelToName(message.Level);
            ConsoleColor color = LogLevelToConsoleColor(message.Level);

            System.Console.Write('[');
            System.Console.ForegroundColor = color;
            System.Console.Write(name);
            System.Console.ResetColor();
            System.Console.WriteLine("] {0}: {1}", message.SawmillName, message.Message);
        }

        private static ConsoleColor LogLevelToConsoleColor(LogLevel level)
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
    }
}
