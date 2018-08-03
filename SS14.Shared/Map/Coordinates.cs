using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using System;

namespace SS14.Shared.Map
{
    /// <summary>
    ///     Coordinates relative to a specific grid.
    /// </summary>
    [Serializable, NetSerializable]
    public struct GridLocalCoordinates : IEquatable<GridLocalCoordinates>
    {
        public readonly GridId GridID;
        public readonly Vector2 Position;

        public float X => Position.X;

        public float Y => Position.Y;

        public IMapGrid Grid => IoCManager.Resolve<IMapManager>().GetGrid(GridID);

        /// <summary>
        ///     The map the grid is currently on. This value is not persistent and may change!
        /// </summary>
        public IMap Map => Grid.Map;

        /// <summary>
        ///     The map ID the grid is currently on. This value is not persistent and may change!
        /// </summary>
        public MapId MapID => Map.Index;

        /// <summary>
        ///     True if these coordinates are relative to a map itself.
        /// </summary>
        public bool IsWorld
        {
            get
            {
                var grid = Grid;
                return grid == grid.Map.DefaultGrid;
            }
        }

        public GridLocalCoordinates(Vector2 argPosition, IMapGrid argGrid)
        {
            Position = argPosition;
            GridID = argGrid.Index;
        }

        public GridLocalCoordinates(Vector2 argPosition, GridId argGrid)
        {
            Position = argPosition;
            GridID = argGrid;
        }

        /// <summary>
        ///     Construct new grid local coordinates relative to the default grid of a map.
        /// </summary>
        public GridLocalCoordinates(Vector2 argPosition, MapId argMap)
        {
            Position = argPosition;
            var mapManager = IoCManager.Resolve<IMapManager>();
            GridID = mapManager.GetMap(argMap).DefaultGrid.Index;
        }

        /// <summary>
        ///     Construct new grid local coordinates relative to the default grid of a map.
        /// </summary>
        public GridLocalCoordinates(Vector2 argPosition, IMap argMap)
        {
            Position = argPosition;
            GridID = argMap.DefaultGrid.Index;
        }

        public GridLocalCoordinates(float X, float Y, IMapGrid argGrid)
        {
            Position = new Vector2(X, Y);
            GridID = argGrid.Index;
        }

        public GridLocalCoordinates(float X, float Y, GridId argGrid)
        {
            Position = new Vector2(X, Y);
            GridID = argGrid;
        }

        /// <summary>
        ///     Construct new grid local coordinates relative to the default grid of a map.
        /// </summary>
        public GridLocalCoordinates(float X, float Y, MapId argMap) : this(new Vector2(X, Y), argMap)
        {
        }

        /// <summary>
        ///     Construct new grid local coordinates relative to the default grid of a map.
        /// </summary>
        public GridLocalCoordinates(float X, float Y, IMap argMap) : this(new Vector2(X, Y), argMap)
        {
        }

        public bool IsValidLocation()
        {
            var mapMan = IoCManager.Resolve<IMapManager>();
            return mapMan.GridExists(GridID);
        }

        public GridLocalCoordinates ConvertToGrid(IMapGrid argGrid)
        {
            return new GridLocalCoordinates(Position + Grid.WorldPosition - argGrid.WorldPosition, argGrid);
        }

        public GridLocalCoordinates ToWorld()
        {
            return ConvertToGrid(Map.DefaultGrid);
        }

        public GridLocalCoordinates Offset(Vector2 offset)
        {
            return new GridLocalCoordinates(Position + offset, GridID);
        }

        public bool InRange(GridLocalCoordinates localpos, float range)
        {
            if (localpos.MapID != MapID)
            {
                return false;
            }

            return ((localpos.ToWorld().Position - ToWorld().Position).LengthSquared < range * range);
        }

        public bool InRange(GridLocalCoordinates localpos, int range)
        {
            return InRange(localpos, (float)range);
        }

        public GridLocalCoordinates Translated(Vector2 offset)
        {
            return new GridLocalCoordinates(Position + offset, GridID);
        }

        public override string ToString()
        {
            return $"Grid={GridID}, X={Position.X:N2}, Y={Position.Y:N2}";
        }

        public bool Equals(GridLocalCoordinates other)
        {
            return GridID.Equals(other.GridID) && Position.Equals(other.Position);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GridLocalCoordinates && Equals((GridLocalCoordinates)obj);
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
        public static bool operator ==(GridLocalCoordinates self, GridLocalCoordinates other)
        {
            return self.Equals(other);
        }

        /// <summary>
        ///     Tests for value inequality between two LocalCoordinates.
        /// </summary>
        public static bool operator !=(GridLocalCoordinates self, GridLocalCoordinates other)
        {
            return !(self == other);
        }
    }

    public struct ScreenCoordinates
    {
        public readonly Vector2 Position;

        public float X => Position.X;

        public float Y => Position.Y;

        public ScreenCoordinates(Vector2 argPosition)
        {
            Position = argPosition;
        }

        public ScreenCoordinates(float X, float Y)
        {
            Position = new Vector2(X, Y);
        }
    }
}
