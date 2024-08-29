using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

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
        EntityUid? target;

        if (args.Length == 3)
        {
            if (!NetEntity.TryParse(args[2], out var targetNet) ||
                !_entities.TryGetEntity(targetNet, out target))
            {
                shell.WriteError($"Unable to find entity {args[1]}");
                return;
            }
        }
        else
        {
            if (!_entities.TryGetComponent(shell.Player?.AttachedEntity, out TransformComponent? xform)
                || xform.ParentUid is not { Valid: true } parent)
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

        var physicsSystem = _entities.System<SharedPhysicsSystem>();
        physicsSystem.SetAngularDamping(target.Value, physics, drag);
        physicsSystem.SetAngularVelocity(target.Value, speed, body: physics);
    }
}
