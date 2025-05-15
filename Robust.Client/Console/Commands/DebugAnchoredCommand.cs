#if DEBUG
using Robust.Client.Debugging;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    public sealed class DebugAnchoredCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly DebugAnchoringSystem _system = default!;

        public override string Command => "showanchored";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _system.Enabled ^= true;
        }
    }
}
#endif
