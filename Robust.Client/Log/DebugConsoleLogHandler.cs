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
            if (sawmillName == "CON")
                return;

            var formatted = new FormattedMessage.Builder();
            var robustLevel = message.Level.ToRobust();
            formatted.PushColor(Color.DarkGray);
            formatted.AddText("[");
            formatted.PushColor(LogLevelToColor(robustLevel));
            formatted.AddText(LogMessage.LogLevelToName(robustLevel));
            formatted.Pop();
            formatted.AddText($"] {sawmillName}: ");
            formatted.Pop();
            formatted.PushColor(Color.LightGray);
            formatted.AddText(message.RenderMessage());
            formatted.Pop();
            if (message.Exception != null)
            {
                formatted.AddText("\n");
                formatted.AddText(message.Exception.ToString());
            }
            Console.AddFormattedLine(formatted.Build());
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
