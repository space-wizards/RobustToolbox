using Robust.Client.Debugging;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Robust.Client.Console.Commands
{
    public sealed class PhysicsOverlayCommands : IConsoleCommand
    {
        public string Command => "physics";
        public string Description => $"{Command} <contactnormals / contactpoints / shapes>";
        public string Help => $"{Command} <overlay>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine($"Invalid number of args supplied");
                return;
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
                    shell.WriteLine($"{args[0]} is not a recognised overlay");
                    return;
            }

            return;
        }
    }
}
