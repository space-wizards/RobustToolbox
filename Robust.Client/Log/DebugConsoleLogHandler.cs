using Robust.Client.Console;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Serilog.Events;

namespace Robust.Client.Log
{
    /// <summary>
    ///     Writes logs to the in-game debug console.
    /// </summary>
    class DebugConsoleLogHandler : ILogHandler
    {
        readonly IClientConsoleHost Console;

        public DebugConsoleLogHandler(IClientConsoleHost console)
        {
            Console = console;
        }

        public void Log(string sawmillName, LogEvent message)
        {
            var formatted = new FormattedMessage(8);
            var robustLevel = message.Level.ToRobust();
            formatted.PushColor(Color.DarkGray);
            formatted.AddText("[");
            formatted.PushColor(LogLevelToColor(robustLevel));
            formatted.AddText(LogMessage.LogLevelToName(robustLevel));
            formatted.Pop();
            formatted.AddText($"] {sawmillName}: ");
            formatted.Pop();
            formatted.AddText(message.RenderMessage());
            if (message.Exception != null)
            {
                formatted.AddText("\n");
                formatted.AddText(message.Exception.ToString());
            }
            Console.AddFormattedLine(formatted);
        }

        private static Color LogLevelToColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Verbose => Color.Green,
                LogLevel.Debug => Color.Blue,
                LogLevel.Info => Color.Cyan,
                LogLevel.Warning => Color.Yellow,
                LogLevel.Error => Color.Red,
                LogLevel.Fatal => Color.Red,
                _ => Color.White
            };
        }
    }
}
