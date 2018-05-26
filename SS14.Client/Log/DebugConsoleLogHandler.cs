using SS14.Client.Interfaces.Console;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Log;

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

        public void Log(LogMessage message)
        {
            Console.AddLine($"[{message.LogLevelToName()}] {message.SawmillName}: {message.Message}");
        }
    }
}
