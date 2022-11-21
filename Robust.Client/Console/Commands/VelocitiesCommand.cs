using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    public sealed class VelocitiesCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _entitySystems = default!;

        public override string Command => "showvelocities";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _entitySystems.GetEntitySystem<VelocityDebugSystem>().Enabled ^= true;
        }
    }
}
