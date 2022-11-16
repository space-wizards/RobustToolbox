#if DEBUG
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Robust.Client.Console.Commands
{
    internal sealed class LightDebugCommand : LocalizedCommands
    {
        public override string Command => "lightbb";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<DebugLightTreeSystem>().Enabled ^= true;
        }
    }
}
#endif
