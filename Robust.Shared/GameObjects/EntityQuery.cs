using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
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
            if (predicate == null)
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
            DebugTools.Assert(typeof(IComponent).IsAssignableFrom(componentType), "componentType must inherit IComponent");

            ComponentType = componentType;
        }

        /// <inheritdoc />
        public bool Match(IEntity entity) => entity.HasComponent(ComponentType);

        /// <inheritdoc />
        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.ComponentManager.GetAllComponents(ComponentType, true).Select(component => component.Owner);
        }
    }

    /// <summary>
    ///     An entity query that will match one type of component.
    ///     This the fastest and most common query, and should be the default choice.
    /// </summary>
    /// <typeparamref name="T">Type of component to match.</typeparamref>
    [PublicAPI]
    public class TypeEntityQuery<T> : IEntityQuery where T : IComponent
    {
        public bool Match(IEntity entity) => entity.HasComponent<T>();

        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.ComponentManager.EntityQuery<T>(true).Select(component => component.Owner);
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
            if(Entity.TryGetComponent<IPhysicsComponent>(out var physics))
            {
                return physics.MapID == entity.Transform.MapID && physics.WorldAABB.Contains(entity.Transform.WorldPosition);
            }
            return false;
        }

        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.GetEntities().Where(entity => Match(entity));
        }
    }

    /// <summary>
    ///     An entity query that will match entities that have all of the provided components.
    /// </summary>
    [PublicAPI]
    public class MultipleTypeEntityQuery : IEntityQuery
    {
        private readonly List<Type> ComponentTypes;

        /// <summary>
        ///     Constructs a new instance of <c>MultipleTypeEntityQuery</c>.
        /// </summary>
        /// <param name="componentTypes">List of component types to match.</param>
        public MultipleTypeEntityQuery(List<Type> componentTypes)
        {
            foreach (var componentType in componentTypes)
            {
                DebugTools.Assert(typeof(IComponent).IsAssignableFrom(componentType), "componentType must inherit IComponent");
            }

            ComponentTypes = componentTypes;
        }

        /// <inheritdoc />
        public bool Match(IEntity entity)
        {
            foreach (var componentType in ComponentTypes)
            {
                if (!entity.HasComponent(componentType))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.GetEntities(new TypeEntityQuery(ComponentTypes.First())).Where(entity => Match(entity));
        }
    }
}
