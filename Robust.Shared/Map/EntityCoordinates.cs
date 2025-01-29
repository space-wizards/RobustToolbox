using System;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     A set of coordinates relative to another entity.
    /// </summary>
    [PublicAPI]
    public readonly struct EntityCoordinates : IEquatable<EntityCoordinates>, ISpanFormattable
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

        public EntityCoordinates()
        {
            EntityId = EntityUid.Invalid;
            Position = Vector2.Zero;
        }

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

        [Obsolete("Use SharedTransformSystem.ToMapCoordinates()")]
        public MapCoordinates ToMap(IEntityManager entityManager, SharedTransformSystem transformSystem)
        {
            return transformSystem.ToMapCoordinates(this);
        }

        [Obsolete("Use SharedTransformSystem.ToMapCoordinates()")]
        public Vector2 ToMapPos(IEntityManager entityManager, SharedTransformSystem transformSystem)
        {
            return ToMap(entityManager, transformSystem).Position;
        }

        [Obsolete("Use SharedTransformSystem.ToCoordinates()")]
        public static EntityCoordinates FromMap(EntityUid entity, MapCoordinates coordinates, SharedTransformSystem transformSystem, IEntityManager? entMan = null)
        {
            return transformSystem.ToCoordinates(entity, coordinates);
        }

        [Obsolete("Use SharedTransformSystem.ToCoordinates()")]
        public static EntityCoordinates FromMap(IMapManager mapManager, MapCoordinates coordinates)
        {
            return IoCManager.Resolve<IEntityManager>().System<SharedTransformSystem>().ToCoordinates(coordinates);
        }

        /// <summary>
        ///     Converts this set of coordinates to Vector2i.
        /// </summary>
        public Vector2i ToVector2i(
            IEntityManager entityManager,
            IMapManager mapManager,
            SharedTransformSystem transformSystem)
        {
            if(!IsValid(entityManager))
                return new Vector2i();

            var mapSystem = entityManager.System<SharedMapSystem>();
            var gridIdOpt = transformSystem.GetGrid(this);
            if (gridIdOpt is { } gridId && gridId.IsValid())
            {
                var grid = entityManager.GetComponent<MapGridComponent>(gridId);
                return mapSystem.GetTileRef(gridId, grid, this).GridIndices;
            }

            var vec = transformSystem.ToMapCoordinates(this);

            return new Vector2i((int)MathF.Floor(vec.X), (int)MathF.Floor(vec.Y));
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
        [Obsolete("Use SharedTransformSystem.WithEntityId()")]
        public EntityCoordinates WithEntityId(IEntityManager entityManager, EntityUid entityId)
        {
            if (!entityManager.EntityExists(entityId))
                return new EntityCoordinates(entityId, Vector2.Zero);

            return WithEntityId(entityId);
        }

        /// <summary>
        ///     Returns a new set of EntityCoordinates local to a new entity.
        /// </summary>
        /// <param name="entity">The entity that the new coordinates will be local to</param>
        /// <returns>A new set of EntityCoordinates local to a new entity.</returns>
        [Obsolete("Use SharedTransformSystem.WithEntityId()")]
        public EntityCoordinates WithEntityId(EntityUid entity, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            return WithEntityId(entity, entMan.System<SharedTransformSystem>(), entMan);
        }

        /// <summary>
        ///     Returns a new set of EntityCoordinates local to a new entity.
        /// </summary>
        /// <param name="entity">The entity that the new coordinates will be local to</param>
        /// <returns>A new set of EntityCoordinates local to a new entity.</returns>
        [Obsolete("Use SharedTransformSystem.WithEntityId()")]
        public EntityCoordinates WithEntityId(
            EntityUid entity,
            SharedTransformSystem transformSystem,
            IEntityManager? entMan = null)
        {
            return transformSystem.WithEntityId(this, entity);
        }

        /// <summary>
        ///     Returns the Grid EntityUid these coordinates are on.
        ///     If none of the ancestors are a grid, returns null instead.
        /// </summary>
        /// <param name="entityManager"></param>
        /// <returns>Grid EntityUid this entity is on or null</returns>
        [Obsolete("Use SharedTransformSystem.GetGrid()")]
        public EntityUid? GetGridUid(IEntityManager entityManager)
        {
            return !IsValid(entityManager) ? null : entityManager.GetComponent<TransformComponent>(EntityId).GridUid;
        }

        /// <summary>
        ///     Returns the Map Id these coordinates are on.
        ///     If the relative entity is not valid, returns <see cref="MapId.Nullspace"/> instead.
        /// </summary>
        /// <param name="entityManager"></param>
        /// <returns>Map Id these coordinates are on or <see cref="MapId.Nullspace"/></returns>
        [Obsolete("Use SharedTransformSystem.GetMapId()")]
        public MapId GetMapId(IEntityManager entityManager)
        {
            return !IsValid(entityManager) ? MapId.Nullspace : entityManager.GetComponent<TransformComponent>(EntityId).MapID;
        }

        /// <summary>
        ///     Returns the Map Id these coordinates are on.
        ///     If the relative entity is not valid, returns null instead.
        /// </summary>
        /// <param name="entityManager"></param>
        /// <returns>Map Id these coordinates are on or null</returns>
        [Obsolete("Use SharedTransformSystem.GetMap()")]
        public EntityUid? GetMapUid(IEntityManager entityManager)
        {
            return !IsValid(entityManager) ? null : entityManager.GetComponent<TransformComponent>(EntityId).MapUid;
        }

        /// <summary>
        /// Offsets the position by a given vector. This happens in local space.
        /// </summary>
        /// <param name="position">The vector to offset by local to the entity.</param>
        /// <returns>Newly offset coordinates.</returns>
        [Pure]
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
        [Obsolete("Use TransformSystem.InRange()")]
        public bool InRange(IEntityManager entityManager, EntityCoordinates otherCoordinates, float range)
        {
            return InRange(entityManager, entityManager.System<SharedTransformSystem>(), otherCoordinates, range);
        }

        [Obsolete("Use TransformSystem.InRange()")]
        public bool InRange(
            IEntityManager entityManager,
            SharedTransformSystem transformSystem,
            EntityCoordinates otherCoordinates,
            float range)
        {
            return transformSystem.InRange(this, otherCoordinates, range);
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
            return TryDistance(
                entityManager,
                entityManager.System<SharedTransformSystem>(),
                otherCoordinates,
                out distance);
        }

        /// <summary>
        ///     Tries to calculate the distance between two sets of coordinates.
        /// </summary>
        /// <returns>True if it was possible to calculate the distance</returns>
        public bool TryDistance(
            IEntityManager entityManager,
            SharedTransformSystem transformSystem,
            EntityCoordinates otherCoordinates,
            out float distance)
        {
            if (TryDelta(entityManager, transformSystem, otherCoordinates, out var delta))
            {
                distance = delta.Length();
                return true;
            }

            distance = 0f;
            return false;
        }

        /// <summary>
        ///     Tries to calculate the distance vector between two sets of coordinates.
        /// </summary>
        /// <returns>True if it was possible to calculate the distance</returns>
        public bool TryDelta(
            IEntityManager entityManager,
            SharedTransformSystem transformSystem,
            EntityCoordinates otherCoordinates,
            out Vector2 delta)
        {
            delta = Vector2.Zero;

            if (!IsValid(entityManager) || !otherCoordinates.IsValid(entityManager))
                return false;

            if (EntityId == otherCoordinates.EntityId)
            {
                delta = Position - otherCoordinates.Position;
                return true;
            }

            var mapCoordinates = transformSystem.ToMapCoordinates(this);
            var otherMapCoordinates = transformSystem.ToMapCoordinates(otherCoordinates);

            if (mapCoordinates.MapId != otherMapCoordinates.MapId)
                return false;

            delta = mapCoordinates.Position - otherMapCoordinates.Position;
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
            $"EntId={EntityId}, X={Position.X:N2}, Y={Position.Y:N2}");
        }
    }
}
