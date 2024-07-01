using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    public sealed class GridChunkBBCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly GridChunkBoundsDebugSystem _system = default!;

        public override string Command => "showchunkbb";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _system.Enabled ^= true;
        }
    }
}
