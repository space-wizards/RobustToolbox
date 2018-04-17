using SS14.Client.Console;
using SS14.Client.Interfaces.Console;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using System;

namespace SS14.Client.Log
{
    class DebugConsoleLogHandler : ILogHandler
    {
        [Dependency]
        IClientConsole console;

        public DebugConsoleLogHandler()
        {
            IoCManager.InjectDependencies(this);
        }

        public void Log(LogMessage message)
        {
            ((IDebugConsole)console).AddLine($"[{message.LogLevelToName()}] {message.SawmillName}: {message.Message}");
        }
    }
}
