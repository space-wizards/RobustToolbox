using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    public sealed class VelocitiesCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly VelocityDebugSystem _system = default!;

        public override string Command => "showvelocities";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _system.Enabled ^= true;
        }
    }
}
