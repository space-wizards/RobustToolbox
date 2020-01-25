using Robust.Shared.Interfaces.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using JetBrains.Annotations;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     Coordinates relative to a specific grid.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public readonly struct GridCoordinates : IEquatable<GridCoordinates>
    {
        /// <summary>
        ///     Map grid that this position is relative to.
        /// </summary>
        public readonly GridId GridID;

        /// <summary>
        ///     Local Position coordinates relative to the MapGrid.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        ///     Location on the X axis relative to the MapGrid.
        /// </summary>
        public float X => Position.X;

        /// <summary>
        ///     Location on the X axis relative to the MapGrid.
        /// </summary>
        public float Y => Position.Y;

        /// <summary>
        ///     A set of coordinates that is at the origin of an invalid grid.
        ///     This is also the values of an uninitialized struct.
        /// </summary>
        public static readonly GridCoordinates InvalidGrid = new GridCoordinates(0, 0, GridId.Invalid);

        /// <summary>
        ///     Constructs new grid local coordinates.
        /// </summary>
        /// <param name="position">Position relative to the grid.</param>
        /// <param name="grid">Grid the position is relative to.</param>
        public GridCoordinates(Vector2 position, IMapGrid grid)
            : this(position, grid.Index) { }

        /// <summary>
        ///     Constructs new grid local coordinates.
        /// </summary>
        /// <param name="position">Position relative to the grid.</param>
        /// <param name="gridId">ID of the Grid the position is relative to.</param>
        public GridCoordinates(Vector2 position, GridId gridId)
        {
            Position = position;
            GridID = gridId;
        }

        /// <summary>
        ///     Constructs new grid local coordinates.
        /// </summary>
        /// <param name="x">X axis of the position.</param>
        /// <param name="y">Y axis of the position.</param>
        /// <param name="grid">Grid the position is relative to.</param>
        public GridCoordinates(float x, float y, IMapGrid grid)
            : this(new Vector2(x, y), grid.Index) { }

        /// <summary>
        ///     Constructs new grid local coordinates.
        /// </summary>
        /// <param name="x">X axis of the position.</param>
        /// <param name="y">Y axis of the position.</param>
        /// <param name="gridId">ID of the Grid the position is relative to.</param>
        public GridCoordinates(float x, float y, GridId gridId)
            : this(new Vector2(x, y), gridId) { }

        /// <summary>
        ///     Converts this set of coordinates to map coordinates.
        /// </summary>
        public MapCoordinates ToMap(IMapManager mapManager)
        {
            //TODO: Assert GridID is not invalid

            var grid = mapManager.GetGrid(GridID);
            return new MapCoordinates(grid.LocalToWorld(Position), grid.ParentMapId);
        }

        /// <summary>
        ///     Converts this set of coordinates to map coordinate position.
        /// </summary>
        public Vector2 ToMapPos(IMapManager mapManager)
        {
            //TODO: Assert GridID is not invalid

            return mapManager.GetGrid(GridID).LocalToWorld(Position);
        }

        /// <summary>
        ///     Offsets the position by a given vector.
        /// </summary>
        public GridCoordinates Offset(Vector2 offset)
        {
            return new GridCoordinates(Position + offset, GridID);
        }

        /// <summary>
        ///     Checks that these coordinates are within a certain distance of another set.
        /// </summary>
        /// <param name="mapManager">Map manager containing the two GridIds.</param>
        /// <param name="otherCoords">Other set of coordinates to use.</param>
        /// <param name="range">maximum distance between the two sets of coordinates.</param>
        /// <returns>True if the two points are within a given range.</returns>
        public bool InRange(IMapManager mapManager, GridCoordinates otherCoords, float range)
        {
            if (mapManager.GetGrid(otherCoords.GridID).ParentMapId != mapManager.GetGrid(GridID).ParentMapId)
            {
                return false;
            }

            return ((otherCoords.ToMapPos(mapManager) - ToMapPos(mapManager)).LengthSquared < range * range);
        }

        /// <summary>
        ///     Checks that these coordinates are within a certain distance of another set.
        /// </summary>
        /// <param name="mapManager">Map manager containing the two GridIds.</param>
        /// <param name="otherCoords">Other set of coordinates to use.</param>
        /// <param name="range">maximum distance between the two sets of coordinates.</param>
        /// <returns>True if the two points are within a given range.</returns>
        public bool InRange(IMapManager mapManager, GridCoordinates otherCoords, int range)
        {
            return InRange(mapManager, otherCoords, (float) range);
        }

        /// <summary>
        ///     Calculates the distance between two GirdCoordinates.
        /// </summary>
        /// <param name="mapManager">Map manager containing this GridId.</param>
        /// <param name="otherCoords">Other set of coordinates to use.</param>
        /// <returns>Distance between the two points.</returns>
        public float Distance(IMapManager mapManager, GridCoordinates otherCoords)
        {
            return (ToMapPos(mapManager) - otherCoords.ToMapPos(mapManager)).Length;
        }

        /// <summary>
        ///     Offsets the position by another vector.
        /// </summary>
        /// <param name="offset">Vector to translate by.</param>
        /// <returns>Resulting translated coordinates.</returns>
        public GridCoordinates Translated(Vector2 offset)
        {
            return new GridCoordinates(Position + offset, GridID);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Grid={GridID}, X={Position.X:N2}, Y={Position.Y:N2}";
        }

        /// <inheritdoc />
        public bool Equals(GridCoordinates other)
        {
            return GridID.Equals(other.GridID) && Position.Equals(other.Position);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GridCoordinates coords && Equals(coords);
        }

        /// <inheritdoc />
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
            return !self.Equals(other);
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
        public static readonly MapCoordinates Nullspace = new MapCoordinates(Vector2.Zero, MapId.Nullspace);

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
