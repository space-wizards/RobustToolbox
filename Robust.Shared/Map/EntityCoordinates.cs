using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     A set of coordinates relative to another entity.
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
        ///     Location of the X axis local to the entity.
        /// </summary>
        public float X => Position.X;

        /// <summary>
        ///     Location of the Y axis local to the entity.
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

            var transform = entityManager.GetComponent<TransformComponent>(EntityId);
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
        ///    Creates EntityCoordinates given an entity and some MapCoordinates.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If <see cref="entity"/> is not on the same map as the <see cref="coordinates"/>.</exception>
        public static EntityCoordinates FromMap(EntityUid entity, MapCoordinates coordinates)
        {
            var transform = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity);
            if(transform.MapID != coordinates.MapId)
                throw new InvalidOperationException("Entity is not on the same map!");

            var localPos = transform.InvWorldMatrix.Transform(coordinates.Position);
            return new EntityCoordinates(entity, localPos);
        }

        /// <summary>
        ///    Creates EntityCoordinates given an entity Uid and some MapCoordinates.
        /// </summary>
        /// <param name="entityManager">Entity Manager containing the entity Id.</param>
        /// <param name="entityUid"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If <see cref="entityUid"/> is not on the same map as the <see cref="coordinates"/>.</exception>
        public static EntityCoordinates FromMap(IEntityManager entityManager, EntityUid entityUid, MapCoordinates coordinates)
        {
            var entity = entityManager.GetEntity(entityUid);

            return FromMap(entity, coordinates);
        }

        /// <summary>
        ///    Creates a set of EntityCoordinates given some MapCoordinates.
        /// </summary>
        /// <param name="mapManager"></param>
        /// <param name="coordinates"></param>
        public static EntityCoordinates FromMap(IMapManager mapManager, MapCoordinates coordinates)
        {
            var mapId = coordinates.MapId;
            var mapEntity = mapManager.GetMapEntityId(mapId);

            return new EntityCoordinates(mapEntity, coordinates.Position);
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
            if(!entityManager.EntityExists(entityId))
                return new EntityCoordinates(entityId, Vector2.Zero);

            return WithEntityId(entityId);
        }

        /// <summary>
        ///     Returns a new set of EntityCoordinates local to a new entity.
        /// </summary>
        /// <param name="entity">The entity that the new coordinates will be local to</param>
        /// <returns>A new set of EntityCoordinates local to a new entity.</returns>
        public EntityCoordinates WithEntityId(EntityUid entity)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapPos = ToMap(entityManager);

            if(!IsValid(entityManager) || entityManager.GetComponent<TransformComponent>(entity).MapID != mapPos.MapId)
                return new EntityCoordinates(entity, Vector2.Zero);

            var localPos = entityManager.GetComponent<TransformComponent>(entity).InvWorldMatrix.Transform(mapPos.Position);
            return new EntityCoordinates(entity, localPos);
        }

        /// <summary>
        ///     Returns the Grid Id these coordinates are on.
        ///     If none of the ancestors are a grid, returns <see cref="GridId.Invalid"/> grid instead.
        /// </summary>
        /// <param name="entityManager"></param>
        /// <returns>Grid Id this entity is on or <see cref="GridId.Invalid"/></returns>
        public GridId GetGridId(IEntityManager entityManager)
        {
            return !IsValid(entityManager) ? GridId.Invalid : entityManager.GetComponent<TransformComponent>(EntityId).GridID;
        }

        /// <summary>
        ///     Returns the Map Id these coordinates are on.
        ///     If the relative entity is not valid, returns <see cref="MapId.Nullspace"/> instead.
        /// </summary>
        /// <param name="entityManager"></param>
        /// <returns>Map Id these coordinates are on or <see cref="MapId.Nullspace"/></returns>
        public MapId GetMapId(IEntityManager entityManager)
        {
            return !IsValid(entityManager) ? MapId.Nullspace : entityManager.GetComponent<TransformComponent>(EntityId).MapID;
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

            if (EntityId == otherCoordinates.EntityId)
                return (otherCoordinates.Position - Position).LengthSquared < range * range;

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

            if (EntityId == otherCoordinates.EntityId)
            {
                distance = (Position - otherCoordinates.Position).Length;
                return true;
            }

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
        ///     Returns the sum for both coordinates but only if they have the same relative entity.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the relative entities aren't the same</exception>
        public static EntityCoordinates operator +(EntityCoordinates left, EntityCoordinates right)
        {
            if(left.EntityId != right.EntityId)
                throw new ArgumentException("Can't sum EntityCoordinates with different relative entities.");

            return new EntityCoordinates(left.EntityId, left.Position + right.Position);
        }

        /// <summary>
        ///     Returns the difference for both coordinates but only if they have the same relative entity.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the relative entities aren't the same</exception>
        public static EntityCoordinates operator -(EntityCoordinates left, EntityCoordinates right)
        {
            if(left.EntityId != right.EntityId)
                throw new ArgumentException("Can't subtract EntityCoordinates with different relative entities.");

            return new EntityCoordinates(left.EntityId, left.Position - right.Position);
        }

        /// <summary>
        ///     Returns the multiplication of both coordinates but only if they have the same relative entity.
        /// </summary>
        /// <exception cref="ArgumentException">When the relative entities aren't the same</exception>
        public static EntityCoordinates operator *(EntityCoordinates left, EntityCoordinates right)
        {
            if(left.EntityId != right.EntityId)
                throw new ArgumentException("Can't multiply EntityCoordinates with different relative entities.");

            return new EntityCoordinates(left.EntityId, left.Position * right.Position);
        }

        /// <summary>
        ///     Scales the coordinates by a given factor.
        /// </summary>
        /// <exception cref="ArgumentException">When the relative entities aren't the same</exception>
        public static EntityCoordinates operator *(EntityCoordinates left, float right)
        {
            return new(left.EntityId, left.Position * right);
        }

        /// <summary>
        ///     Scales the coordinates by a given factor.
        /// </summary>
        /// <exception cref="ArgumentException">When the relative entities aren't the same</exception>
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
