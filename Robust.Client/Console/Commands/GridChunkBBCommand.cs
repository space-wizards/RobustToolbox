using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Robust.Client.Console.Commands
{
    public class GridChunkBBCommand : IConsoleCommand
    {
        public string Command => "showchunkbb";
        public string Description => "Displays chunk bounds for the purposes of rendering";
        public string Help => $"{Command}";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<GridChunkBoundsDebugSystem>().Enabled ^= true;
        }
    }
}
