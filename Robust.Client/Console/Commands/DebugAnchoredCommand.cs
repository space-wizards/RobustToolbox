#if DEBUG
using Robust.Client.Debugging;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Robust.Client.Console.Commands
{
    public sealed class DebugAnchoredCommand : IConsoleCommand
    {
        public string Command => "showanchored";
        public string Description => $"Shows anchored entities on a particular tile";
        public string Help => $"{Command}";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<DebugAnchoringSystem>().Enabled ^= true;
        }
    }
}
#endif
