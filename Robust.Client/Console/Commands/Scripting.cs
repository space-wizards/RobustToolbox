#if SCRIPTING
using Robust.Client.Interfaces.Console;

namespace Robust.Client.Console.Commands
{
    internal sealed class ScriptConsoleCommand : IConsoleCommand
    {
        public string Command => "csi";
        public string Description => "Opens a C# interactive console.";
        public string Help => "csi";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            new ScriptConsole().OpenCenteredMinSize();

            return false;
        }
    }
}
#endif
