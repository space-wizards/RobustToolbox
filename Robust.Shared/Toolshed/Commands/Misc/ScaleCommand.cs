using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Misc;

/// <summary>
/// Used to change an entity's sprite scale.
/// </summary>
[ToolshedCommand]
public sealed class ScaleCommand : ToolshedCommand
{
    private SharedScaleVisualsSystem? _system;

    [CommandImplementation("set")]
    public IEnumerable<EntityUid> Set([PipedArgument] IEnumerable<EntityUid> input, Vector2 scale)
    {
        _system ??= GetSys<SharedScaleVisualsSystem>();

        foreach (var ent in input)
        {
            _system.SetSpriteScale(ent, scale);
            yield return ent;
        }
    }

    [CommandImplementation("multiply")]
    public IEnumerable<EntityUid> Multiply([PipedArgument] IEnumerable<EntityUid> input, float factor)
    {
        _system ??= GetSys<SharedScaleVisualsSystem>();

        foreach (var ent in input)
        {
            var scale = _system.GetSpriteScale(ent) * factor;
            _system.SetSpriteScale(ent, scale);
            yield return ent;
        }
    }

    [CommandImplementation("get")]
    public IEnumerable<Vector2> Get([PipedArgument] IEnumerable<EntityUid> input)
    {
        _system ??= GetSys<SharedScaleVisualsSystem>();

        foreach (var ent in input)
        {
            yield return _system.GetSpriteScale(ent);
        }
    }
}
