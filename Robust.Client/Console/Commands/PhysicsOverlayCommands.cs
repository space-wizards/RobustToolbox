using Robust.Client.Debugging;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Robust.Client.Console.Commands
{
    public sealed class PhysicsOverlayCommands : IConsoleCommand
    {
        public string Command => "physics";
        public string Description => $"Shows a debug physics overlay. The arg supplied specifies the overlay.";
        public string Help => $"{Command} <contactnormals / contactpoints / shapes / shapeinfo>";
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
                case "shapeinfo":
                    system.Flags ^= PhysicsDebugFlags.ShapeInfo;
                    break;
                default:
                    shell.WriteLine($"{args[0]} is not a recognised overlay");
                    return;
            }

            return;
        }
    }
}
