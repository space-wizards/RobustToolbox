using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
internal sealed class PhysicsCommand : ToolshedCommand
{
    private SharedTransformSystem? _xform = default!;

    [CommandImplementation("velocity")]
    public IEnumerable<float> Velocity([PipedArgument] IEnumerable<EntityUid> input)
    {
        var physQuery = GetEntityQuery<PhysicsComponent>();

        foreach (var ent in input)
        {
            if (!physQuery.TryGetComponent(ent, out var comp))
                continue;

            yield return comp.LinearVelocity.Length();
        }
    }

    [CommandImplementation("parent")]
    public IEnumerable<EntityUid> Parent([PipedArgument] IEnumerable<EntityUid> input)
    {
        _xform ??= GetSys<SharedTransformSystem>();
        return input.Select(x => Comp<TransformComponent>(x).ParentUid);
    }

    [CommandImplementation("angular_velocity")]
    public IEnumerable<float> AngularVelocity([PipedArgument] IEnumerable<EntityUid> input)
    {
        var physQuery = GetEntityQuery<PhysicsComponent>();

        foreach (var ent in input)
        {
            if (!physQuery.TryGetComponent(ent, out var comp))
                continue;

            yield return comp.AngularVelocity;
        }
    }
}
