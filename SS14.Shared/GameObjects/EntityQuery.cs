using SS14.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.IoC;
using SS14.Shared.Interfaces.GameObjects.Components;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    ///     An entity query that will let all entities pass.
    ///     This is the same as matching <c>ITransformComponent</c>, but faster.
    /// </summary>
    [PublicAPI]
    public class AllEntityQuery : IEntityQuery
    {
        /// <inheritdoc />
        public bool Match(IEntity entity) => true;

        /// <inheritdoc />
        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.GetEntities();
        }
    }

    /// <summary>
    ///     An entity query which will match entities based on a predicate.
    ///     If you only want a single type of Component, use <c>TypeEntityQuery</c>.
    /// </summary>
    [PublicAPI]
    public class PredicateEntityQuery : IEntityQuery
    {
        private readonly Predicate<IEntity> Predicate;

        /// <summary>
        ///     Constructs a new instance of <c>PredicateEntityQuery</c>.
        /// </summary>
        /// <param name="predicate"></param>
        public PredicateEntityQuery(Predicate<IEntity> predicate)
        {
            if(Predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            Predicate = predicate;
        }

        /// <inheritdoc />
        public bool Match(IEntity entity) => Predicate(entity);

        /// <inheritdoc />
        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.GetEntities().Where(entity => Predicate(entity));
        }
    }

    /// <summary>
    ///     An entity query that will match one type of component.
    ///     This the fastest and most common query, and should be the default choice.
    /// </summary>
    [PublicAPI]
    public class TypeEntityQuery : IEntityQuery
    {
        private readonly Type ComponentType;

        /// <summary>
        ///     Constructs a new instance of <c>TypeEntityQuery</c>.
        /// </summary>
        /// <param name="componentType">Type of the component to match.</param>
        public TypeEntityQuery(Type componentType)
        {
            ComponentType = componentType;
        }

        /// <inheritdoc />
        public bool Match(IEntity entity) => entity.HasComponent(ComponentType);

        /// <inheritdoc />
        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.ComponentManager.GetAllComponents(ComponentType).Select(component => component.Owner);
        }
    }

    /// <summary>
    ///     An entity query that will match all entities that intersect with the argument entity.
    /// </summary>
    [PublicAPI]
    public class IntersectingEntityQuery : IEntityQuery
    {
        private readonly IEntity Entity;

        /// <summary>
        ///     Constructs a new instance of <c>TypeEntityQuery</c>.
        /// </summary>
        /// <param name="componentType">Type of the component to match.</param>
        public IntersectingEntityQuery(IEntity entity)
        {
            Entity = entity;
        }

        /// <inheritdoc />
        public bool Match(IEntity entity)
        {
            if(Entity.TryGetComponent<ICollidableComponent>(out var collidable))
            {
                return collidable.MapID == entity.Transform.MapID && collidable.WorldAABB.Contains(entity.Transform.WorldPosition);
            }
            return false;
        }

        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.GetEntities().Where(entity => Match(entity));
        }
    }
}
