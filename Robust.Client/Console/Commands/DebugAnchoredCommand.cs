#if DEBUG
using Robust.Client.Debugging;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Robust.Client.Console.Commands
{
    public sealed class DebugAnchoredCommand : LocalizedCommands
    {
        public override string Command => "showanchored";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<DebugAnchoringSystem>().Enabled ^= true;
        }
    }
}
#endif
