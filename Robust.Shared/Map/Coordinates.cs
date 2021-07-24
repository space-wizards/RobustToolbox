using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Map
{
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
        ///     The window which the coordinates are on.
        /// </summary>
        public readonly WindowId Window;

        /// <summary>
        ///     Constructs a new instance of <c>ScreenCoordinates</c>.
        /// </summary>
        /// <param name="position">Position on the rendering screen.</param>
        /// <param name="window">Window for the coordinates.</param>
        public ScreenCoordinates(Vector2 position, WindowId window)
        {
            Position = position;
            Window = window;
        }

        /// <summary>
        ///     Constructs a new instance of <c>ScreenCoordinates</c>.
        /// </summary>
        /// <param name="x">X axis of a position on the screen.</param>
        /// <param name="y">Y axis of a position on the screen.</param>
        /// <param name="window">Window for the coordinates.</param>
        public ScreenCoordinates(float x, float y, WindowId window)
        {
            Position = new Vector2(x, y);
            Window = window;
        }

        public bool IsValid => Window != WindowId.Invalid;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({Position.X}, {Position.Y}, W{Window.Value})";
        }

        /// <inheritdoc />
        public bool Equals(ScreenCoordinates other)
        {
            return Position.Equals(other.Position) && Window == other.Window;
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
            return HashCode.Combine(Position, Window);
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

        public void Deconstruct(out Vector2 pos, out WindowId window)
        {
            pos = Position;
            window = Window;
        }
    }

    /// <summary>
    ///     Coordinates relative to a specific map.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public readonly struct MapCoordinates : IEquatable<MapCoordinates>
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
        public static readonly EntityCoordinates Invalid = new(EntityUid.Invalid, Vector2.Zero);

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
            EntityId = entityId;
            Position = position;
        }

        public EntityCoordinates(EntityUid entityId, float x, float y)
        {
            EntityId = entityId;
            Position = new Vector2(x, y);
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
            if(!IsValid(entityManager))
                return MapCoordinates.Nullspace;

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
        /// <param name="mapManager"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        [Obsolete("Use FromMap(IMapManager mapManager, MapCoordinates coordinates) instead.")]
        public static EntityCoordinates FromMap(IEntityManager entityManager, IMapManager mapManager, MapCoordinates coordinates)
        {
            var mapId = coordinates.MapId;
            var mapEntity = mapManager.GetMapEntity(mapId);

            return new EntityCoordinates(mapEntity.Uid, coordinates.Position);
        }

        /// <summary>
        ///    Creates a set of EntityCoordinates given some MapCoordinates.
        /// </summary>
        /// <param name="mapManager"></param>
        /// <param name="coordinates"></param>
        public static EntityCoordinates FromMap(IMapManager mapManager, MapCoordinates coordinates)
        {
            var mapId = coordinates.MapId;
            var mapEntity = mapManager.GetMapEntity(mapId);

            return new EntityCoordinates(mapEntity.Uid, coordinates.Position);
        }

        /// <summary>
        ///     Converts this set of coordinates to Vector2i.
        /// </summary>
        /// <param name="entityManager"></param>
        /// <param name="mapManager"></param>
        /// <returns></returns>
        public Vector2i ToVector2i(IEntityManager entityManager, IMapManager mapManager)
        {
            if(!IsValid(entityManager))
                return new Vector2i();

            var gridId = GetGridId(entityManager);

            if (gridId != GridId.Invalid)
            {
                return mapManager.GetGrid(gridId).GetTileRef(this).GridIndices;
            }

            var (x, y) = ToMapPos(entityManager);

            return new Vector2i((int)Math.Floor(x), (int)Math.Floor(y));
        }

        /// <summary>
        ///     Returns an new set of EntityCoordinates with the same <see cref="EntityId"/>
        ///     but on a different position.
        /// </summary>
        /// <param name="newPosition">The position the new EntityCoordinates will be in</param>
        /// <returns>A new set of EntityCoordinates with the specified position and same <see cref="EntityId"/> as this one.</returns>
        public EntityCoordinates WithPosition(Vector2 newPosition)
        {
            return new(EntityId, newPosition);
        }

        /// <summary>
        ///     Returns a new set of EntityCoordinates local to a new entity.
        /// </summary>
        /// <param name="entityManager">The Entity Manager holding this entity</param>
        /// <param name="entityId">The entity that the new coordinates will be local to</param>
        /// <returns>A new set of EntityCoordinates local to a new entity.</returns>
        public EntityCoordinates WithEntityId(IEntityManager entityManager, EntityUid entityId)
        {
            if(!entityManager.TryGetEntity(entityId, out var entity))
                return new EntityCoordinates(entityId, Vector2.Zero);

            return WithEntityId(entity);
        }

        /// <summary>
        ///     Returns a new set of EntityCoordinates local to a new entity.
        /// </summary>
        /// <param name="entity">The entity that the new coordinates will be local to</param>
        /// <returns>A new set of EntityCoordinates local to a new entity.</returns>
        public EntityCoordinates WithEntityId(IEntity entity)
        {
            var entityManager = entity.EntityManager;
            var mapPos = ToMap(entity.EntityManager);

            if(!IsValid(entityManager) || entity.Transform.MapID != mapPos.MapId)
                return new EntityCoordinates(entity.Uid, Vector2.Zero);

            var localPos = entity.Transform.InvWorldMatrix.Transform(mapPos.Position);
            return new EntityCoordinates(entity.Uid, localPos);
        }

        /// <summary>
        ///     Returns the Grid Id this entity is on.
        ///     If none of the ancestors are a grid, returns <see cref="GridId.Invalid"/> grid instead.
        /// </summary>
        /// <param name="entityManager">Entity Manager that contains the parent's Id</param>
        /// <returns>Grid Id this entity is on or <see cref="GridId.Invalid"/></returns>
        public GridId GetGridId(IEntityManager entityManager)
        {
            return !IsValid(entityManager) ? GridId.Invalid : GetParent(entityManager).Transform.GridID;
        }

        /// <summary>
        ///     Returns the Map Id this entity is on.
        ///     If the parent entity is not valid, returns <see cref="MapId.Nullspace"/> instead.
        /// </summary>
        /// <param name="entityManager">Entity Manager that contains the parent's Id</param>
        /// <returns>Map Id this entity is on or <see cref="MapId.Nullspace"/></returns>
        public MapId GetMapId(IEntityManager entityManager)
        {
            return !IsValid(entityManager) ? MapId.Nullspace : GetParent(entityManager).Transform.MapID;
        }

        /// <summary>
        ///     Returns the parent entity.
        /// </summary>
        /// <param name="entityManager">Entity Manager containing the parent entity</param>
        /// <returns>Parent entity or throws if entity id doesn't exist</returns>
        public IEntity GetParent(IEntityManager entityManager)
        {
            return entityManager.GetEntity(EntityId);
        }

        /// <summary>
        ///     Attempt to get the parent entity, returning whether or not the entity was gotten.
        /// </summary>
        /// <param name="entityManager">Entity Manager containing the parent entity</param>
        /// <param name="parent">The parent entity or null if not valid</param>
        /// <returns>True if a value was returned, false otherwise.</returns>
        public bool TryGetParent(IEntityManager entityManager, [NotNullWhen(true)] out IEntity? parent)
        {
            parent = null;
            return IsValid(entityManager) && entityManager.TryGetEntity(EntityId, out parent);
        }

        /// <summary>
        /// Offsets the position by a given vector. This happens in local space.
        /// </summary>
        /// <param name="position">The vector to offset by local to the entity.</param>
        /// <returns>Newly offset coordinates.</returns>
        public EntityCoordinates Offset(Vector2 position)
        {
            return new(EntityId, Position + position);
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
            if (!IsValid(entityManager) || !otherCoordinates.IsValid(entityManager))
                return false;

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
            distance = 0f;

            if (!IsValid(entityManager) || !otherCoordinates.IsValid(entityManager))
                return false;

            var mapCoordinates = ToMap(entityManager);
            var otherMapCoordinates = otherCoordinates.ToMap(entityManager);

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
            return new(left.EntityId, left.Position * right);
        }

        /// <summary>
        ///     Scales the coordinates by a given factor.
        /// </summary>
        /// <exception cref="ArgumentException">When the parents aren't the same</exception>
        public static EntityCoordinates operator *(EntityCoordinates left, int right)
        {
            return new(left.EntityId, left.Position * right);
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
