using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

namespace Robust.Server.Console.Commands;

public sealed class SpinCommand : IConsoleCommand
{
    public string Command => "spin";
    public string Description => "Causes an entity to spin. Default entity is the attached player's parent.";
    public string Help => $"{Command} velocity [drag] [entityUid]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError($"Usage: {Help}");
            return;
        }

        float drag = 0;
        if (!float.TryParse(args[0], out var speed) || (args.Length > 1 && !float.TryParse(args[1], out drag)))
        {
            shell.WriteError($"Unable to parse float");
            return;
        }

        var entMan = IoCManager.Resolve<IEntityManager>();

        // get the target
        EntityUid target;
        if (args.Length == 3)
        {
            if (!EntityUid.TryParse(args[1], out target))
            {
                shell.WriteError($"Unable to find entity {args[1]}");
                return;
            }
        }
        else
        {
            if (!entMan.TryGetComponent(shell.Player?.AttachedEntity, out TransformComponent? xform)
                || xform.ParentUid is not EntityUid { Valid: true } parent)
            {
                shell.WriteError($"Cannot find default entity (attached player's parent).");
                return;
            }
            target = parent;
        }

        // Try get physics
        if (!entMan.TryGetComponent(target, out PhysicsComponent physics))
        {
            shell.WriteError($"Target entity is incorporeal");
            return;
        }

        if (physics.BodyType != BodyType.Dynamic)
        {
            shell.WriteError($"Target entity is not dynamic");
            return;
        }

        physics.AngularDamping = drag;
        physics.AngularVelocity = speed;
    }
}
