using System;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Map;

/// <summary>
///     This structure contains the data for an individual Tile in a <c>MapGrid</c>.
/// </summary>
[PublicAPI, Serializable]
public readonly struct Tile : IEquatable<Tile>, ISpanFormattable
{
    /// <summary>
    ///     Internal type ID of this tile.
    /// </summary>
    public readonly int TypeId;

    /// <summary>
    ///     Custom flags for additional tile-data.
    /// </summary>
    public readonly byte Flags;

    /// <summary>
    /// Variant of this tile to render.
    /// </summary>
    public readonly byte Variant;

    /// <summary>
    ///     An empty tile that can be compared against.
    /// </summary>
    public static readonly Tile Empty = new(0);

    /// <summary>
    ///     Is this tile space (empty)?
    /// </summary>
    public bool IsEmpty => TypeId == 0;

    /// <summary>
    ///     Creates a new instance of a grid tile.
    /// </summary>
    /// <param name="typeId">Internal type ID.</param>
    /// <param name="flags">Custom tile flags for usage.</param>
    /// <param name="variant">The visual variant this tile is using.</param>
    public Tile(int typeId, byte flags = 0, byte variant = 0)
    {
        TypeId = typeId;
        Flags = flags;
        Variant = variant;
    }

    /// <summary>
    ///     Check for equality by value between two objects.
    /// </summary>
    public static bool operator ==(Tile a, Tile b)
    {
        return a.Equals(b);
    }

    /// <summary>
    ///     Check for inequality by value between two objects.
    /// </summary>
    public static bool operator !=(Tile a, Tile b)
    {
        return !a.Equals(b);
    }

    /// <summary>
    /// Generates String representation of this Tile.
    /// </summary>
    /// <returns>String representation of this Tile.</returns>
    public override string ToString()
    {
        return $"Tile {TypeId}, {Flags}, {Variant}";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString();
    }

    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        return FormatHelpers.TryFormatInto(
            destination,
            out charsWritten,
            $"Tile {TypeId}, {Flags}, {Variant}");
    }

    /// <inheritdoc />
    public bool Equals(Tile other)
    {
        return TypeId == other.TypeId && Flags == other.Flags && Variant == other.Variant;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        return obj is Tile other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return (TypeId.GetHashCode() * 397) ^ Flags.GetHashCode() ^ Variant.GetHashCode();
        }
    }

    [Pure]
    public Tile WithVariant(byte variant)
    {
        return new Tile(TypeId, Flags, variant);
    }

    [Pure]
    public Tile WithFlag(byte flag)
    {
        return new Tile(TypeId, flags: flag, variant: Variant);
    }
}

public sealed class TileFlagLayer {}

