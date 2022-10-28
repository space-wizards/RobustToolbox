using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    public sealed class VelocitiesCommand : LocalizedCommands
    {
        public override string Command => "showvelocities";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<VelocityDebugSystem>().Enabled ^= true;
        }
    }
}
