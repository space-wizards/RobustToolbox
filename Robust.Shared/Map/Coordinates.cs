using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using JetBrains.Annotations;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     Coordinates relative to a specific grid.
    /// </summary>
    [Serializable, NetSerializable]
    public readonly struct GridCoordinates : IEquatable<GridCoordinates>
    {
        public readonly GridId GridID;
        public readonly Vector2 Position;

        public float X => Position.X;

        public float Y => Position.Y;

        public static readonly GridCoordinates Nullspace = new GridCoordinates(0, 0, GridId.Nullspace);

        public GridCoordinates(Vector2 argPosition, IMapGrid argGrid)
            : this(argPosition, argGrid.Index) { }

        public GridCoordinates(Vector2 argPosition, GridId argGrid)
        {
            Position = argPosition;
            GridID = argGrid;
        }

        /// <summary>
        ///     Construct new grid local coordinates relative to the default grid of a map.
        /// </summary>
        public GridCoordinates(Vector2 argPosition, IMap argMap)
            : this(argPosition, argMap.DefaultGrid.Index) { }

        public GridCoordinates(float X, float Y, IMapGrid argGrid)
        : this(new Vector2(X, Y), argGrid.Index)
        {
        }

        public GridCoordinates(float X, float Y, GridId argGrid)
        : this(new Vector2(X, Y), argGrid)
        {
        }

        /// <summary>
        ///     Construct new grid local coordinates relative to the default grid of a map.
        /// </summary>
        public GridCoordinates(float X, float Y, IMap argMap) : this(new Vector2(X, Y), argMap)
        {
        }

        public GridCoordinates ConvertToGrid(IMapGrid argGrid)
        {
            return new GridCoordinates(Position + IoCManager.Resolve<IMapManager>().GetGrid(GridID).WorldPosition - argGrid.WorldPosition, argGrid);
        }

        public GridCoordinates ToWorld()
        {
            return ConvertToGrid(IoCManager.Resolve<IMapManager>().GetGrid(GridID).Map.DefaultGrid);
        }

        public GridCoordinates Offset(Vector2 offset)
        {
            return new GridCoordinates(Position + offset, GridID);
        }

        public bool InRange(GridCoordinates localpos, float range)
        {
            if (IoCManager.Resolve<IMapManager>().GetGrid(localpos.GridID).Map.Index != IoCManager.Resolve<IMapManager>().GetGrid(GridID).Map.Index)
            {
                return false;
            }

            return ((localpos.ToWorld().Position - ToWorld().Position).LengthSquared < range * range);
        }

        public bool InRange(GridCoordinates localpos, int range)
        {
            return InRange(localpos, (float) range);
        }

        public float Distance(GridCoordinates other)
        {
            return (ToWorld().Position - other.ToWorld().Position).Length;
        }

        public GridCoordinates Translated(Vector2 offset)
        {
            return new GridCoordinates(Position + offset, GridID);
        }

        public override string ToString()
        {
            return $"Grid={GridID}, X={Position.X:N2}, Y={Position.Y:N2}";
        }

        public bool Equals(GridCoordinates other)
        {
            return GridID.Equals(other.GridID) && Position.Equals(other.Position);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GridCoordinates && Equals((GridCoordinates) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = GridID.GetHashCode();
                hashCode = (hashCode * 397) ^ Position.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        ///     Tests for value equality between two LocalCoordinates.
        /// </summary>
        public static bool operator ==(GridCoordinates self, GridCoordinates other)
        {
            return self.Equals(other);
        }

        /// <summary>
        ///     Tests for value inequality between two LocalCoordinates.
        /// </summary>
        public static bool operator !=(GridCoordinates self, GridCoordinates other)
        {
            return !(self == other);
        }
    }

    /// <summary>
    ///     Contains the coordinates of a position on the rendering screen.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public readonly struct ScreenCoordinates : IEquatable<ScreenCoordinates>
    {
        /// <summary>
        ///     Position on the rendering screen.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        ///     Screen position on the X axis.
        /// </summary>
        public float X => Position.X;

        /// <summary>
        ///     Screen position on the Y axis.
        /// </summary>
        public float Y => Position.Y;

        /// <summary>
        ///     Constructs a new instance of <c>ScreenCoordinates</c>.
        /// </summary>
        /// <param name="position">Position on the rendering screen.</param>
        public ScreenCoordinates(Vector2 position)
        {
            Position = position;
        }

        /// <summary>
        ///     Constructs a new instance of <c>ScreenCoordinates</c>.
        /// </summary>
        /// <param name="x">X axis of a position on the screen.</param>
        /// <param name="y">Y axis of a position on the screen.</param>
        public ScreenCoordinates(float x, float y)
        {
            Position = new Vector2(x, y);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Position.ToString();
        }

        /// <inheritdoc />
        public bool Equals(ScreenCoordinates other)
        {
            return Position.Equals(other.Position);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is ScreenCoordinates other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(ScreenCoordinates a, ScreenCoordinates b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(ScreenCoordinates a, ScreenCoordinates b)
        {
            return !a.Equals(b);
        }
    }

    /// <summary>
    ///     Coordinates relative to a specific map.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public readonly struct MapCoordinates : IEquatable<MapCoordinates>
    {
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
            return $"({Position.X}, {Position.Y}, map: {MapId})";
        }

        /// <inheritdoc />
        public bool Equals(MapCoordinates other)
        {
            return Position.Equals(other.Position) && MapId.Equals(other.MapId);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is MapCoordinates other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ MapId.GetHashCode();
            }
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(MapCoordinates a, MapCoordinates b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(MapCoordinates a, MapCoordinates b)
        {
            return !a.Equals(b);
        }
    }
}
