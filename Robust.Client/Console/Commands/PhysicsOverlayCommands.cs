using Robust.Client.Debugging;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Client.Console.Commands
{
    public sealed class PhysicsOverlayCommands : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _entitySystems = default!;

        public override string Command => "physics";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 1), ("currentAmount", args.Length)));
                return;
            }

            var system = _entitySystems.GetEntitySystem<DebugPhysicsSystem>();

            switch (args[0])
            {
                case "aabbs":
                    system.Flags ^= PhysicsDebugFlags.AABBs;
                    break;
                case "com":
                    system.Flags ^= PhysicsDebugFlags.COM;
                    break;
                case "contactnormals":
                    system.Flags ^= PhysicsDebugFlags.ContactNormals;
                    break;
                case "contactpoints":
                    system.Flags ^= PhysicsDebugFlags.ContactPoints;
                    break;
                case "distance":
                    system.Flags ^= PhysicsDebugFlags.Distance;
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
                    shell.WriteError(Loc.GetString("cmd-physics-overlay", ("overlay", args[0])));
                    return;
            }

            return;
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length != 1) return CompletionResult.Empty;

            return CompletionResult.FromOptions(new[]
            {
                "aabbs",
                "com",
                "contactnormals",
                "contactpoints",
                "distance",
                "joints",
                "shapeinfo",
                "shapes",
            });
        }
    }
}
