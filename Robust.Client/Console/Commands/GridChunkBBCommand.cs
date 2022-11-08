using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Robust.Client.Console.Commands
{
    public sealed class GridChunkBBCommand : LocalizedCommands
    {
        public override string Command => "showchunkbb";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<GridChunkBoundsDebugSystem>().Enabled ^= true;
        }
    }
}
