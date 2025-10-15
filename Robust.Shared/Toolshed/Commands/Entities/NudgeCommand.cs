using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class NudgeCommand : ToolshedCommand
{
    private SharedTransformSystem? _xform;

    private void Nudge(EntityUid uid, float deltaX, float deltaY)
    {
        _xform ??= GetSys<SharedTransformSystem>();

        var xform = Transform(uid);

        _xform.SetLocalPosition(uid, xform.LocalPosition + new Vector2(deltaX, deltaY), xform);
    }

    [CommandImplementation("up")]
    void NudgeUpPiped([PipedArgument] IEnumerable<EntityUid> input, float deltaY) => Nudge(input, 0, deltaY);
    [CommandImplementation("right")]
    void NudgeRightPiped([PipedArgument] IEnumerable<EntityUid> input, float deltaX) => Nudge(input, deltaX, 0);
    [CommandImplementation("down")]
    void NudgeDownPiped([PipedArgument] IEnumerable<EntityUid> input, float deltaY) => Nudge(input, 0, -deltaY);
    [CommandImplementation("left")]
    void NudgeLeftPiped([PipedArgument] IEnumerable<EntityUid> input, float deltaX) => Nudge(input, -deltaX, 0);

    [CommandImplementation("xpiped")]
    // I struggle to find a use case for this but someone might try making a turing machine or some shit idk
    public void NudgeXPiped(int entity, [PipedArgument] IEnumerable<float> deltaX)
    {
        foreach (var dX in deltaX)
        {
            Nudge(entity, dX, 0);
        }
    }
    [CommandImplementation("ypiped")]
    public void NudgeYPiped(int entity, [PipedArgument] IEnumerable<float> deltaY)
    {
        foreach (var dY in deltaY)
        {
            Nudge(entity, 0, dY);
        }
    }

    public void Nudge([PipedArgument] IEnumerable<EntityUid> input, float deltaX, float deltaY)
    {
        foreach (var entityUid in input)
        {
            Nudge(entityUid, deltaX, deltaY);
        }
    }

    public void Nudge(int entity, float deltaX, float deltaY)
    {
        if (!NetEntity.TryParse(entity.ToString(), out var netEntity)
            || !EntityManager.TryGetEntity(netEntity, out var uid)
            || !EntityManager.EntityExists(uid))
        {
            throw new ArgumentException($"Entity {entity} could not be found.");
        }

        Nudge(uid.Value, deltaX, deltaY);
    }
}
