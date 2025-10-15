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

    [CommandImplementation("up")]
    void NudgeUpPiped(int entity, float deltaY) => Nudge(entity, 0, deltaY);
    [CommandImplementation("right")]
    void NudgeRightPiped(int entity, float deltaX) => Nudge(entity, deltaX, 0);
    [CommandImplementation("down")]
    void NudgeDownPiped(int entity, float deltaY) => Nudge(entity, 0, -deltaY);
    [CommandImplementation("left")]
    void NudgeLeftPiped(int entity, float deltaX) => Nudge(entity, -deltaX, 0);

    [CommandImplementation("xy")]
    public void Nudge([PipedArgument] IEnumerable<EntityUid> input, float deltaX, float deltaY)
    {
        foreach (var entityUid in input)
        {
            Nudge(entityUid, deltaX, deltaY);
        }
    }

    [CommandImplementation("xy")]
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
