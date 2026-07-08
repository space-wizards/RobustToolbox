using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    public sealed partial class ShowPlayerVelocityCommand : LocalizedEntityCommands
    {
        [Dependency] private ShowPlayerVelocityDebugSystem _system = default!;

        public override string Command => "showplayervelocity";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _system.Enabled ^= true;
        }
    }
}
