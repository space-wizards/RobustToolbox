using Robust.Shared.Interfaces.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     Coordinates relative to a specific grid.
    /// </summary>
    [Obsolete("Use EntityCoordinates instead.")]
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
        ///     Converts this set of coordinates to map indices.
        /// </summary>
        public MapIndices ToMapIndices(IMapManager mapManager)
        {
            return mapManager.GetGrid(GridID).GetTileRef(this).GridIndices;
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
        public override bool Equals(object? obj)
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
        public override bool Equals(object? obj)
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
            return $"Map={MapId}, X={Position.X:N2}, Y={Position.Y:N2}";
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

            return ((otherCoords.Position - Position).LengthSquared < range * range);
        }

        /// <inheritdoc />
        public bool Equals(MapCoordinates other)
        {
            return Position.Equals(other.Position) && MapId.Equals(other.MapId);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
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
    }

    /// <summary>
    /// A set of coordinates relative to another entity.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public readonly struct EntityCoordinates : IEquatable<EntityCoordinates>
    {
        /// <summary>
        ///     ID of the entity that this position is relative to.
        /// </summary>
        public readonly EntityUid EntityId;

        /// <summary>
        ///     Position in the entity's local space.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        ///     Location of the X axis local to the parent.
        /// </summary>
        public float X => Position.X;

        /// <summary>
        ///     Location of the Y axis local to the parent.
        /// </summary>
        public float Y => Position.Y;

        /// <summary>
        ///     Constructs a new instance of <see cref="EntityCoordinates"/>.
        /// </summary>
        /// <param name="entityId">ID of the entity that this position is relative to.</param>
        /// <param name="position">Position in the entity's local space.</param>
        public EntityCoordinates(EntityUid entityId, Vector2 position)
        {
            if(!entityId.IsValid())
                throw new ArgumentException("Invalid ID", nameof(entityId));

            if(!float.IsFinite(position.X) || !float.IsFinite(position.Y))
                throw new ArgumentOutOfRangeException(nameof(position), "Vector is not finite.");

            EntityId = entityId;
            Position = position;
        }

        /// <summary>
        ///     Verifies that this set of coordinates can be currently resolved to a location.
        /// </summary>
        /// <param name="entityManager">Entity Manager containing the entity Id.</param>
        /// <returns><see langword="true" /> if this set of coordinates can be currently resolved to a location, otherwise <see langword="false" />.</returns>
        public bool IsValid(IEntityManager entityManager)
        {
            if (!EntityId.IsValid() || !entityManager.EntityExists(EntityId))
                return false;

            if (!float.IsFinite(Position.X) || !float.IsFinite(Position.Y))
                return false;

            return true;
        }

        /// <summary>
        ///     Transforms this set of coordinates from the entity's local space to the map space.
        /// </summary>
        /// <param name="entityManager">Entity Manager containing the entity Id.</param>
        /// <returns></returns>
        public MapCoordinates ToMap(IEntityManager entityManager)
        {
            var transform = entityManager.GetEntity(EntityId).Transform;
            var worldPos = transform.WorldMatrix.Transform(Position);
            return new MapCoordinates(worldPos, transform.MapID);
        }

        /// <summary>
        ///    Transform this set of coordinates from the entity's local space to the map space.
        /// </summary>
        /// <param name="entityManager">Entity Manager containing the entity Id.</param>
        /// <returns></returns>
        public Vector2 ToMapPos(IEntityManager entityManager)
        {
            return ToMap(entityManager).Position;
        }

        /// <summary>
        ///    Creates EntityCoordinates given a parent and some MapCoordinates.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If <see cref="parent"/> is not on the same map as the <see cref="coordinates"/>.</exception>
        public static EntityCoordinates FromMap(IEntity parent, MapCoordinates coordinates)
        {
            if(parent.Transform.MapID != coordinates.MapId)
                throw new InvalidOperationException("Entity is not on the same map!");

            var localPos = parent.Transform.InvWorldMatrix.Transform(coordinates.Position);
            return new EntityCoordinates(parent.Uid, localPos);
        }

        /// <summary>
        ///    Creates EntityCoordinates given a parent Uid and some MapCoordinates.
        /// </summary>
        /// <param name="entityManager">Entity Manager containing the entity Id.</param>
        /// <param name="parentUid"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If <see cref="parentUid"/> is not on the same map as the <see cref="coordinates"/>.</exception>
        public static EntityCoordinates FromMap(IEntityManager entityManager, EntityUid parentUid, MapCoordinates coordinates)
        {
            var parent = entityManager.GetEntity(parentUid);

            return FromMap(parent, coordinates);
        }

        /// <summary>
        ///    Creates a set of EntityCoordinates given some MapCoordinates.
        /// </summary>
        /// <param name="entityManager"></param>
        /// <param name="mapManager"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public static EntityCoordinates FromMap(IEntityManager entityManager, IMapManager mapManager, MapCoordinates coordinates)
        {
            var mapId = coordinates.MapId;
            var mapEntity = mapManager.GetMapEntity(mapId);

            return new EntityCoordinates(mapEntity.Uid, coordinates.Position);
        }

        /// <summary>
        ///     Converts a set of <seealso cref="EntityCoordinates"/> into a set of <seealso cref="GridCoordinates"/>.
        /// </summary>
        /// <param name="entityManager">Entity manager that contains the <see cref="EntityId"/>.</param>
        /// <param name="coordinates">Coordinates being converted to <see cref="GridCoordinates"/>. The <see cref="EntityId"/>
        /// will be resolved to it's corresponding <see cref="GridId"/>.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Will be thrown if the <see cref="EntityId"/> is not a grid.</exception>
        public GridCoordinates ToGrid(IEntityManager entityManager, EntityCoordinates coordinates)
        {
            if (!entityManager.TryGetEntity(coordinates.EntityId, out var gridEntity)
                || !gridEntity.TryGetComponent<IMapGridComponent>(out var gridComp))
            {
                throw new InvalidOperationException("The entity is not a grid!");
            }

            return new GridCoordinates(coordinates.Position, gridComp.GridIndex);
        }

        /// <summary>
        ///     Creates a set of EntityCoordinates given some GridCoordinates.
        /// </summary>
        /// <param name="mapManager"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public static EntityCoordinates FromGrid(IMapManager mapManager, GridCoordinates coordinates)
        {
            var grid = mapManager.GetGrid(coordinates.GridID);
            return new EntityCoordinates(grid.GridEntityId, coordinates.Position);
        }

        /// <summary>
        ///     Returns the Grid Id this entity is on.
        ///     If none of the ancestors are a grid, returns <see cref="GridId.Invalid"/> grid instead.
        /// </summary>
        /// <param name="entityManager">Entity Manager that contains the parent's Id</param>
        /// <returns>Grid Id this entity is on or <see cref="GridId.Invalid"/></returns>
        public GridId GetGridId(IEntityManager entityManager)
        {
            var parent = EntityId;
            while (parent.IsValid())
            {
                var entity = entityManager.GetEntity(parent);
                if (entity.TryGetComponent(out IMapGridComponent? mapGrid))
                {
                    return mapGrid.GridIndex;
                }

                var newParentUid = entity.Transform.ParentUid;
                if(!newParentUid.IsValid())
                    return GridId.Invalid;
            }

            return GridId.Invalid;
        }

        /// <summary>
        /// Offsets the position by a given vector. This happens in local space.
        /// </summary>
        /// <param name="position">The vector to offset by local to the entity.</param>
        /// <returns>Newly offset coordinates.</returns>
        public EntityCoordinates Offset(Vector2 position)
        {
            return new EntityCoordinates(EntityId, Position + position);
        }

        /// <summary>
        ///     Compares two sets of coordinates to see if they are in range of each other.
        /// </summary>
        /// <param name="entityManager">Entity Manager containing the two entity Ids.</param>
        /// <param name="otherCoordinates">Other set of coordinates to use.</param>
        /// <param name="range">maximum distance between the two sets of coordinates.</param>
        /// <returns>True if the two points are within a given range.</returns>
        public bool InRange(IEntityManager entityManager, EntityCoordinates otherCoordinates, float range)
        {
            var mapCoordinates = ToMap(entityManager);
            var otherMapCoordinates = otherCoordinates.ToMap(entityManager);

            return mapCoordinates.InRange(otherMapCoordinates, range);
        }

        /// <summary>
        ///     Tries to calculate the distance between two sets of coordinates.
        /// </summary>
        /// <param name="entityManager"></param>
        /// <param name="otherCoordinates"></param>
        /// <param name="distance"></param>
        /// <returns>True if it was possible to calculate the distance</returns>
        public bool TryDistance(IEntityManager entityManager, EntityCoordinates otherCoordinates, out float distance)
        {
            var mapCoordinates = ToMap(entityManager);
            var otherMapCoordinates = otherCoordinates.ToMap(entityManager);

            distance = 0f;

            if (mapCoordinates.MapId != otherMapCoordinates.MapId)
                return false;

            distance = (mapCoordinates.Position - otherMapCoordinates.Position).Length;
            return true;
        }

        #region IEquatable

        /// <inheritdoc />
        public bool Equals(EntityCoordinates other)
        {
            return EntityId.Equals(other.EntityId) && Position.Equals(other.Position);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is EntityCoordinates other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(EntityId, Position);
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(EntityCoordinates left, EntityCoordinates right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(EntityCoordinates left, EntityCoordinates right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Operators

        /// <summary>
        ///     Returns the sum for both coordinates but only if they have the same parent.
        /// </summary>
        /// <exception cref="ArgumentException">When the parents aren't the same</exception>
        public static EntityCoordinates operator +(EntityCoordinates left, EntityCoordinates right)
        {
            if(left.EntityId != right.EntityId)
                throw new ArgumentException("Can't sum EntityCoordinates with different parents.");

            return new EntityCoordinates(left.EntityId, left.Position + right.Position);
        }

        /// <summary>
        ///     Returns the difference for both coordinates but only if they have the same parent.
        /// </summary>
        /// <exception cref="ArgumentException">When the parents aren't the same</exception>
        public static EntityCoordinates operator -(EntityCoordinates left, EntityCoordinates right)
        {
            if(left.EntityId != right.EntityId)
                throw new ArgumentException("Can't substract EntityCoordinates with different parents.");

            return new EntityCoordinates(left.EntityId, left.Position - right.Position);
        }

        /// <summary>
        ///     Returns the multiplication of both coordinates but only if they have the same parent.
        /// </summary>
        /// <exception cref="ArgumentException">When the parents aren't the same</exception>
        public static EntityCoordinates operator *(EntityCoordinates left, EntityCoordinates right)
        {
            if(left.EntityId != right.EntityId)
                throw new ArgumentException("Can't multiply EntityCoordinates with different parents.");

            return new EntityCoordinates(left.EntityId, left.Position * right.Position);
        }

        /// <summary>
        ///     Scales the coordinates by a given factor.
        /// </summary>
        /// <exception cref="ArgumentException">When the parents aren't the same</exception>
        public static EntityCoordinates operator *(EntityCoordinates left, float right)
        {
            return new EntityCoordinates(left.EntityId, left.Position * right);
        }

        /// <summary>
        ///     Scales the coordinates by a given factor.
        /// </summary>
        /// <exception cref="ArgumentException">When the parents aren't the same</exception>
        public static EntityCoordinates operator *(EntityCoordinates left, int right)
        {
            return new EntityCoordinates(left.EntityId, left.Position * right);
        }

        #endregion

        /// <summary>
        /// Deconstructs the object into it's fields.
        /// </summary>
        /// <param name="entId">ID of the entity that this position is relative to.</param>
        /// <param name="localPos">Position in the entity's local space.</param>
        public void Deconstruct(out EntityUid entId, out Vector2 localPos)
        {
            entId = EntityId;
            localPos = Position;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"EntId={EntityId}, X={Position.X:N2}, Y={Position.Y:N2}";
        }
    }
}
