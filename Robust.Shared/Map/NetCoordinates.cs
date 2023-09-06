using System;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Map;

/// <summary>
///     A networked version of <see cref="EntityCoordinates"/>
/// </summary>
[PublicAPI]
[Serializable, NetSerializable]
public readonly struct NetCoordinates : IEquatable<NetCoordinates>, ISpanFormattable
{
    public static readonly NetCoordinates Invalid = new(NetEntity.Invalid, Vector2.Zero);

    /// <summary>
    ///     Networked ID of the entity that this position is relative to.
    /// </summary>
    public readonly NetEntity NetEntity;

    /// <summary>
    ///     Position in the entity's local space.
    /// </summary>
    public readonly Vector2 Position;

    /// <summary>
    ///     Location of the X axis local to the entity.
    /// </summary>
    public float X => Position.X;

    /// <summary>
    ///     Location of the Y axis local to the entity.
    /// </summary>
    public float Y => Position.Y;

    public NetCoordinates(NetEntity netEntity, Vector2 position)
    {
        NetEntity = netEntity;
        Position = position;
    }

    public NetCoordinates(NetEntity netEntity, float x, float y)
    {
        NetEntity = netEntity;
        Position = new Vector2(x, y);
    }
    #region IEquatable

    /// <inheritdoc />
    public bool Equals(NetCoordinates other)
    {
        return NetEntity.Equals(other.NetEntity) && Position.Equals(other.Position);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is NetCoordinates other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(NetEntity, Position);
    }

    #endregion

    /// <summary>
    /// Deconstructs the object into it's fields.
    /// </summary>
    /// <param name="entId">ID of the entity that this position is relative to.</param>
    /// <param name="localPos">Position in the entity's local space.</param>
    public void Deconstruct(out NetEntity entId, out Vector2 localPos)
    {
        entId = NetEntity;
        localPos = Position;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"NetEntity={NetEntity}, X={Position.X:N2}, Y={Position.Y:N2}";
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        return FormatHelpers.TryFormatInto(
            destination,
            out charsWritten,
            $"NetEntity={NetEntity}, X={Position.X:N2}, Y={Position.Y:N2}");
    }
}
