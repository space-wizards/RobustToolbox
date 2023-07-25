using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.RTShell.Commands.Physics;

[ConsoleCommand]
internal sealed class PhysicsCommand : ConsoleCommand
{
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

    public IEnumerable<EntityUid> Parents([PipedArgument] IEnumerable<EntityUid> input)
    {
        foreach (var VARIABLE in input)
        {

        }

        throw new NotImplementedException();
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
