using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Misc;

/// <summary>
/// Used to change an entity's sprite scale.
/// </summary>
[ToolshedCommand]
public sealed class PolymorphCommand : ToolshedCommand
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

    [CommandImplementation("set")]
    public EntityUid Set([PipedArgument] EntityUid input, Vector2 scale)
    {
        _system ??= GetSys<SharedScaleVisualsSystem>();

        _system.SetSpriteScale(input, scale);
        return input;
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

    [CommandImplementation("multiply")]
    public EntityUid Multiply([PipedArgument] EntityUid input, float factor)
    {
        _system ??= GetSys<SharedScaleVisualsSystem>();

        var scale = _system.GetSpriteScale(input) * factor;
        _system.SetSpriteScale(input, scale);
        return input;
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

    [CommandImplementation("get")]
    public Vector2 Get([PipedArgument] EntityUid input)
    {
        _system ??= GetSys<SharedScaleVisualsSystem>();

        return _system.GetSpriteScale(input);
    }
}
