using System;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     Coordinates relative to a specific map.
    /// </summary>
    [PublicAPI, DataRecord]
    [Serializable, NetSerializable]
    public readonly record struct MapCoordinates : ISpanFormattable
    {
        public static readonly MapCoordinates Nullspace = new(Vector2.Zero, MapId.Nullspace);

        /// <summary>
        ///     World Position coordinates.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        ///     Map identifier relevant to this position.
        /// </summary>
        public readonly MapId MapId;

        /// <summary>
        ///     World position on the X axis.
        /// </summary>
        public float X => Position.X;

        /// <summary>
        ///     World position on the Y axis.
        /// </summary>
        public float Y => Position.Y;

        /// <summary>
        ///     Constructs a new instance of <c>MapCoordinates</c>.
        /// </summary>
        /// <param name="position">World position coordinates.</param>
        /// <param name="mapId">Map identifier relevant to this position.</param>
        public MapCoordinates(Vector2 position, MapId mapId)
        {
            Position = position;
            MapId = mapId;
        }

        /// <summary>
        ///     Constructs a new instance of <c>MapCoordinates</c>.
        /// </summary>
        /// <param name="x">World position coordinate on the X axis.</param>
        /// <param name="y">World position coordinate on the Y axis.</param>
        /// <param name="mapId">Map identifier relevant to this position.</param>
        public MapCoordinates(float x, float y, MapId mapId)
            : this(new Vector2(x, y), mapId) { }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Map={MapId}, X={Position.X:N2}, Y={Position.Y:N2}";
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
                $"Map={MapId}, X={Position.X:N2}, Y={Position.Y:N2}");
        }

        /// <summary>
        ///     Checks that these coordinates are within a certain distance of another set.
        /// </summary>
        /// <param name="otherCoords">Other set of coordinates to use.</param>
        /// <param name="range">maximum distance between the two sets of coordinates.</param>
        /// <returns>True if the two points are within a given range.</returns>
        public bool InRange(MapCoordinates otherCoords, float range)
        {
            if (otherCoords.MapId != MapId)
            {
                return false;
            }

            return (otherCoords.Position - Position).LengthSquared() < range * range;
        }

        /// <summary>
        /// Used to deconstruct this object into a tuple.
        /// </summary>
        /// <param name="x">World position coordinate on the X axis.</param>
        /// <param name="y">World position coordinate on the Y axis.</param>
        public void Deconstruct(out float x, out float y)
        {
            x = X;
            y = Y;
        }

        /// <summary>
        /// Used to deconstruct this object into a tuple.
        /// </summary>
        /// <param name="mapId">Map identifier relevant to this position.</param>
        /// <param name="x">World position coordinate on the X axis.</param>
        /// <param name="y">World position coordinate on the Y axis.</param>
        public void Deconstruct(out MapId mapId, out float x, out float y)
        {
            mapId = MapId;
            x = X;
            y = Y;
        }

        /// <summary>
        /// Used to get a copy of the coordinates with an offset.
        /// </summary>
        /// <param name="offset">Offset to apply to these coordinates</param>
        /// <returns>A copy of these coordinates, but offset.</returns>
        public MapCoordinates Offset(Vector2 offset)
        {
            return new MapCoordinates(Position + offset, MapId);
        }

        /// <summary>
        /// Used to get a copy of the coordinates with an offset.
        /// </summary>
        /// <param name="x">X axis offset to apply to these coordinates</param>
        /// <param name="y">Y axis offset to apply to these coordinates</param>
        /// <returns>A copy of these coordinates, but offset.</returns>
        public MapCoordinates Offset(float x, float y)
        {
            return Offset(new Vector2(x, y));
        }
    }
}
