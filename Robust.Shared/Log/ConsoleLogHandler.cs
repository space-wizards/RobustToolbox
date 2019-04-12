using System;
using Robust.Shared.Interfaces.Log;

namespace Robust.Shared.Log
{
    /// <summary>
    ///     Log handler that prints to console.
    /// </summary>
    public sealed class ConsoleLogHandler : ILogHandler
    {
        private readonly object locker = new object();

        public void Log(in LogMessage message)
        {
            var name = LogMessage.LogLevelToName(message.Level);
            var color = LogLevelToConsoleColor(message.Level);

            lock (locker)
            {
                System.Console.Write('[');
                System.Console.ForegroundColor = color;
                System.Console.Write(name);
                System.Console.ResetColor();
                System.Console.WriteLine("] {0}: {1}", message.SawmillName, message.Message);
            }
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
