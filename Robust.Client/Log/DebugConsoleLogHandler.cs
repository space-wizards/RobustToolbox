using System.Collections.Generic;
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

            var formatted = new List<Section>();
            var robustLevel = message.Level.ToRobust();
            formatted.AddRange(new []
                    {
                        new Section() { Color=Color.DarkGray.ToArgb(), Content="[" },
                        new Section() { Color=LogLevelToColor(robustLevel).ToArgb(), Content=LogMessage.LogLevelToName(robustLevel) },
                        new Section() { Color=Color.DarkGray.ToArgb(), Content=$"] {sawmillName}: " },
                        new Section() { Color=Color.LightGray.ToArgb(), Content=message.RenderMessage() }
                    }
            );

            if (message.Exception != null)
                formatted.Add(new Section() { Content="\n" + message.Exception.ToString() });

            Console.AddFormattedLine(new FormattedMessage(formatted.ToArray()));
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
