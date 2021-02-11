using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Console;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.Console.Commands
{
    public class VelocitiesCommand : IConsoleCommand
    {
        public string Command => "showvelocities";
        public string Description => "Displays your angular and linear velocities";
        public string Help => $"{Command}";
        public bool Execute(IDebugConsole console, params string[] args)
        {
            EntitySystem.Get<VelocityDebugSystem>().Enabled ^= true;
            return false;
        }
    }
}
