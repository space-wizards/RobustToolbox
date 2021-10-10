using Robust.Client.Debugging;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Robust.Client.Console.Commands
{
    public sealed class PhysicsOverlayCommands : IConsoleCommand
    {
        public string Command => "physics";
        public string Description => $"Shows a debug physics overlay. The arg supplied specifies the overlay.";
        public string Help => $"{Command} <aabbs / contactnormals / contactpoints / joints / shapeinfo / shapes>";
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
                case "aabbs":
                    system.Flags ^= PhysicsDebugFlags.AABBs;
                    break;
                case "contactnormals":
                    system.Flags ^= PhysicsDebugFlags.ContactNormals;
                    break;
                case "contactpoints":
                    system.Flags ^= PhysicsDebugFlags.ContactPoints;
                    break;
                case "joints":
                    system.Flags ^= PhysicsDebugFlags.Joints;
                    break;
                case "shapeinfo":
                    system.Flags ^= PhysicsDebugFlags.ShapeInfo;
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
