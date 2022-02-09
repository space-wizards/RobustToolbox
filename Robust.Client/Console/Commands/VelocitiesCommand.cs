using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Robust.Client.Console.Commands
{
    public sealed class VelocitiesCommand : IConsoleCommand
    {
        public string Command => "showvelocities";
        public string Description => "Displays your angular and linear velocities";
        public string Help => $"{Command}";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<VelocityDebugSystem>().Enabled ^= true;
        }
    }
}
