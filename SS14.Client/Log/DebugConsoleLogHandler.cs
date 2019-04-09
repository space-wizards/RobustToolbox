using SS14.Client.Interfaces.Console;
using SS14.Client.Utility;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.Log
{
    /// <summary>
    ///     Writes logs to the in-game debug console.
    /// </summary>
    class DebugConsoleLogHandler : ILogHandler
    {
        readonly IDebugConsole Console;

        public DebugConsoleLogHandler(IDebugConsole console)
        {
            Console = console;
        }

        public void Log(in LogMessage message)
        {
            var formatted = new FormattedMessage(8);
            formatted.PushColor(Color.DarkGray);
            formatted.AddText("[");
            formatted.PushColor(LogLevelToColor(message.Level));
            formatted.AddText(message.LogLevelToName());
            formatted.Pop();
            formatted.AddText($"] {message.SawmillName}: ");
            formatted.Pop();
            formatted.AddText(message.Message);
            Console.AddFormattedLine(formatted);
        }

        private static Color LogLevelToColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return Color.Blue;

                case LogLevel.Info:
                    return Color.Cyan;

                case LogLevel.Warning:
                    return Color.Yellow;

                case LogLevel.Error:
                    return Color.Red;

                case LogLevel.Fatal:
                    return Color.Red;

                default:
                    return Color.White;
            }
        }
    }
}
