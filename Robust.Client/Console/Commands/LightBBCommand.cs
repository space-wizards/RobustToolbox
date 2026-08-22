#if DEBUG
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    internal sealed partial class LightDebugCommand : LocalizedEntityCommands
    {
        [Dependency] private DebugLightTreeSystem _system = default!;

        public override string Command => "lightbb";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _system.Enabled ^= true;
        }
    }
}
#endif
