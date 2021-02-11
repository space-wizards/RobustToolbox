using Robust.Client.Debugging;
using Robust.Client.Interfaces.Console;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.Console.Commands
{
    public sealed class PhysicsOverlayCommands : IConsoleCommand
    {
        public string Command => "physics";
        public string Description => $"{Command} <contactnormals / contactpoints / shapes>";
        public string Help => $"{Command} <overlay>";
        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 1)
            {
                console.AddLine($"Invalid number of args supplied");
                return false;
            }

            var system = EntitySystem.Get<DebugPhysicsSystem>();

            switch (args[0])
            {
                case "contactnormals":
                    system.Flags ^= PhysicsDebugFlags.ContactNormals;
                    break;
                case "contactpoints":
                    system.Flags ^= PhysicsDebugFlags.ContactPoints;
                    break;
                case "shapes":
                    system.Flags ^= PhysicsDebugFlags.Shapes;
                    break;
                default:
                    console.AddLine($"{args[0]} is not a recognised overlay");
                    return false;
            }

            return false;
        }
    }
}
