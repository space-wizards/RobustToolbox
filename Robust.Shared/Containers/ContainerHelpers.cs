using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Helper functions for the container system.
    /// </summary>
    [PublicAPI]
    public static class ContainerHelpers
    {
        /// <summary>
        /// Am I inside a container? Only checks the direct parent. To see if the entity, or any parent entity, is
        /// inside a container, use <see cref="ContainerSystem.IsEntityOrParentInContainer"/>
        /// </summary>
        /// <param name="entity">Entity that might be inside a container.</param>
        /// <returns>If the entity is inside of a container.</returns>
        [Obsolete("Use ContainerSystem.IsEntityInContainer() instead")]
        public static bool IsInContainer(this EntityUid entity,
            IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            return (entMan.GetComponent<MetaDataComponent>(entity).Flags & MetaDataFlags.InContainer) == MetaDataFlags.InContainer;
        }

        /// <summary>
        /// Tries to find the container manager that this entity is inside (if any).
        /// </summary>
        /// <param name="entity">Entity that might be inside a container.</param>
        /// <param name="manager">The container manager that this entity is inside of.</param>
        /// <returns>If a container manager was found.</returns>
        public static bool TryGetContainerMan(this EntityUid entity, [NotNullWhen(true)] out IContainerManager? manager, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            DebugTools.Assert(entMan.EntityExists(entity));

            var parentTransform = entMan.GetComponent<TransformComponent>(entity).Parent;
            if (parentTransform != null && TryGetManagerComp(parentTransform.Owner, out manager, entMan) && manager.ContainsEntity(entity))
                return true;

            manager = default;
            return false;
        }

        /// <summary>
        /// Tries to find the container that this entity is inside (if any).
        /// </summary>
        /// <param name="entity">Entity that might be inside a container.</param>
        /// <param name="container">The container that this entity is inside of.</param>
        /// <returns>If a container was found.</returns>
        [Obsolete("Use ContainerSystem.TryGetContainingContainer() instead")]
        public static bool TryGetContainer(this EntityUid entity, [NotNullWhen(true)] out IContainer? container, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            DebugTools.Assert(entMan.EntityExists(entity));

            if (TryGetContainerMan(entity, out var manager, entMan))
                return manager.TryGetContainer(entity, out container);

            container = default;
            return false;
        }

        /// <summary>
        /// Attempts to remove an entity from its container, if any.
        /// <see cref="SharedContainerSystem.TryRemoveFromContainer"/>
        /// </summary>
        /// <param name="entity">Entity that might be inside a container.</param>
        /// <param name="force">Whether to forcibly remove the entity from the container.</param>
        /// <param name="wasInContainer">Whether the entity was actually inside a container or not.</param>
        /// <returns>If the entity could be removed. Also returns false if it wasn't inside a container.</returns>
        [Obsolete("Use SharedContainerSystem.TryRemoveFromContainer() instead")]
        public static bool TryRemoveFromContainer(this EntityUid entity, bool force, out bool wasInContainer, IEntityManager? entMan = null)
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedContainerSystem>()
                .TryRemoveFromContainer(entity, force, out wasInContainer);
        }

        /// <summary>
        /// Attempts to remove an entity from its container, if any.
        /// <see cref="SharedContainerSystem.TryRemoveFromContainer"/>
        /// </summary>
        /// <param name="entity">Entity that might be inside a container.</param>
        /// <param name="force">Whether to forcibly remove the entity from the container.</param>
        /// <returns>If the entity could be removed. Also returns false if it wasn't inside a container.</returns>
        [Obsolete("Use SharedContainerSystem.TryRemoveFromContainer() instead")]
        public static bool TryRemoveFromContainer(this EntityUid entity, bool force = false, IEntityManager? entMan = null)
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedContainerSystem>()
                .TryRemoveFromContainer(entity, force);
        }

        /// <summary>
        /// Attempts to remove all entities in a container.
        /// <see cref="SharedContainerSystem.EmptyContainer"/>
        /// </summary>
        [Obsolete("Use SharedContainerSystem.EmptyContainer() instead")]
        public static void EmptyContainer(this IContainer container, bool force = false, EntityCoordinates? moveTo = null,
            bool attachToGridOrMap = false, IEntityManager? entMan = null)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedContainerSystem>()
                .EmptyContainer(container, force, moveTo, attachToGridOrMap);
        }

        /// <summary>
        /// Attempts to remove and delete all entities in a container.
        /// <see cref="SharedContainerSystem.CleanContainer"/>
        /// </summary>
        [Obsolete("Use SharedContainerSystem.CleanContainer() instead")]
        public static void CleanContainer(this IContainer container, IEntityManager? entMan = null)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedContainerSystem>()
                .CleanContainer(container);
        }

        /// <summary>
        /// <see cref="SharedContainerSystem.AttachParentToContainerOrGrid"/>
        /// </summary>
        [Obsolete("Use SharedContainerSystem.AttachParentToContainerOrGrid() instead")]
        public static void AttachParentToContainerOrGrid(this TransformComponent transform, IEntityManager? entMan = null)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedContainerSystem>()
                .AttachParentToContainerOrGrid(transform);
        }

        /// <summary>
        /// <see cref="SharedContainerSystem.TryGetManagerComp"/>
        /// </summary>
        [Obsolete("Use SharedContainerSystem.TryGetManagerComp() instead")]
        private static bool TryGetManagerComp(this EntityUid entity, [NotNullWhen(true)] out IContainerManager? manager, IEntityManager? entMan = null)
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedContainerSystem>()
                .TryGetManagerComp(entity, out manager);
        }

        /// <summary>
        /// Shortcut method to make creation of containers easier.
        /// Creates a new container on the entity and gives it back to you.
        /// </summary>
        /// <param name="entity">The entity to create the container for.</param>
        /// <param name="containerId"></param>
        /// <returns>The new container.</returns>
        /// <exception cref="ArgumentException">Thrown if there already is a container with the specified ID.</exception>
        /// <seealso cref="IContainerManager.MakeContainer{T}(string)" />
        [Obsolete("Use ContainerSystem.MakeContainer() instead")]
        public static T CreateContainer<T>(this EntityUid entity, string containerId, IEntityManager? entMan = null)
            where T : IContainer
        {
            IoCManager.Resolve(ref entMan);
            var containermanager = entMan.EnsureComponent<ContainerManagerComponent>(entity);
            return containermanager.MakeContainer<T>(containerId);
        }

        [Obsolete("Use ContainerSystem.EnsureContainer() instead")]
        public static T EnsureContainer<T>(this EntityUid entity, string containerId, IEntityManager? entMan = null)
            where T : IContainer
        {
            IoCManager.Resolve(ref entMan);
            return EnsureContainer<T>(entity, containerId, out _, entMan);
        }

        [Obsolete("Use ContainerSystem.EnsureContainer() instead")]
        public static T EnsureContainer<T>(this EntityUid entity, string containerId, out bool alreadyExisted, IEntityManager? entMan = null)
            where T : IContainer
        {
            IoCManager.Resolve(ref entMan);
            var containerManager = entMan.EnsureComponent<ContainerManagerComponent>(entity);

            if (!containerManager.TryGetContainer(containerId, out var existing))
            {
                alreadyExisted = false;
                return containerManager.MakeContainer<T>(containerId);
            }

            if (!(existing is T container))
            {
                throw new InvalidOperationException(
                    $"The container exists but is of a different type: {existing.GetType()}");
            }

            alreadyExisted = true;
            return container;
        }
    }
}
