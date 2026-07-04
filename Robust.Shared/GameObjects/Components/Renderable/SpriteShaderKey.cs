using System;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Key for sprite shaders SortedDictionary
/// </summary>
/// <param name="id">The id of the shader. Used for equality checks and hash comparison</param>
/// <param name="renderOrder">Render order of the shader, shaders with higher render order will draw later</param>
/// <remarks>
///     This will be automatically validated by <see cref="SpriteShaderKeySerializer"/>.
///     It is serialized into an integer which represents RenderOrder, id is automatically generated in this case
/// </remarks>
[Serializable]
public readonly struct SpriteShaderKey(string id, int renderOrder = 0)
    : IEquatable<SpriteShaderKey>, IComparable<SpriteShaderKey>
{
    [ViewVariables]
    public readonly string Id = id;

    [ViewVariables]
    public readonly int RenderOrder = renderOrder;

    public bool Equals(SpriteShaderKey other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is SpriteShaderKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public int CompareTo(SpriteShaderKey other)
    {
        return Id == other.Id ? 0 : RenderOrder.CompareTo(other.RenderOrder);
    }
}
