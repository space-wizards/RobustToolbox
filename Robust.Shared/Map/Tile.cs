using System;
using JetBrains.Annotations;
using Robust.Shared.Maths;
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
    public readonly ushort TypeId;

    /// <summary>
    ///     Rendering flags.
    /// </summary>
    public readonly TileRenderFlag Flags;

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
    /// <param name="flags">Flags used by toolbox's rendering.</param>
    /// <param name="variant">The visual variant this tile is using.</param>
    public Tile(ushort typeId, TileRenderFlag flags = 0, byte variant = 0)
    {
        TypeId = typeId;
        Flags = flags;
        Variant = variant;
    }

    /// <summary>
    ///     Explicit conversion of <c>Tile</c> to <c>uint</c> . This should only
    ///     be used in special cases like serialization. Do NOT use this in
    ///     content.
    /// </summary>
    public static explicit operator uint(Tile tile)
    {
        return ((uint)tile.TypeId << 16) | (uint)tile.Flags << 8 | tile.Variant;
    }

    /// <summary>
    ///     Explicit conversion of <c>uint</c> to <c>Tile</c> . This should only
    ///     be used in special cases like serialization. Do NOT use this in
    ///     content.
    /// </summary>
    public static explicit operator Tile(uint tile)
    {
        return new(
            (ushort)(tile >> 16),
            (TileRenderFlag)(tile >> 8),
            (byte)tile
        );
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

    /// <summary>
    ///     Convert Direction to rotational render flag.
    /// </summary>
    public static TileRenderFlag DirectionToTileFlag(Direction dir)
    {
        // TODO Support Mirroring somehow

        return dir switch
        {
            Direction.East => TileRenderFlag.Rotate90,
            Direction.North => TileRenderFlag.Rotate180,
            Direction.West => TileRenderFlag.Rotate270,
            _ => TileRenderFlag.Identity,
        };
    }
}

/// <summary>
///     Flags used to modify how a given tile gets rendered. Currently just used for rotation & mirroring.
/// </summary>
[Flags]
public enum TileRenderFlag : byte
{
    // First two bits determine rotation
    // Third bit determines whether to mirror along the x coordinate (before rotating).

    Identity = 0,
    Rotate90 = 1,
    Rotate180 = 2,
    Rotate270 = 3,
    FlipX = 4,
    FlipXRotate90 = 5,
    FlipXRotate180 = 6,
    FlipXRotate270 = 7,

    // Maybe use remaining bits for animation frame offset?
}
