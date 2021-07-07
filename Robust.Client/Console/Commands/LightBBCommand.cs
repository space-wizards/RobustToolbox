#if DEBUG
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Robust.Client.Console.Commands
{
    internal sealed class LightDebugCommand : IConsoleCommand
    {
        public string Command => "lightbb";
        public string Description => "Toggles whether to show light bounding boxes";
        public string Help => $"{Command}";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<DebugLightTreeSystem>().Enabled ^= true;
        }
    }
}
#endif
