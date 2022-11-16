using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Robust.Server.Console.Commands;

public sealed class SpinCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override string Command => "spin";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
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

        // get the target
        EntityUid target;
        if (args.Length == 3)
        {
            if (!EntityUid.TryParse(args[2], out target))
            {
                shell.WriteError($"Unable to find entity {args[1]}");
                return;
            }
        }
        else
        {
            if (!_entities.TryGetComponent(shell.Player?.AttachedEntity, out TransformComponent? xform)
                || xform.ParentUid is not EntityUid { Valid: true } parent)
            {
                shell.WriteError($"Cannot find default entity (attached player's parent).");
                return;
            }
            target = parent;
        }

        // Try get physics
        if (!_entities.TryGetComponent(target, out PhysicsComponent? physics))
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
