using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        /// Am I inside a container?
        /// </summary>
        /// <param name="entity">Entity that might be inside a container.</param>
        /// <returns>If the entity is inside of a container.</returns>
        public static bool IsInContainer(this EntityUid entity, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            DebugTools.Assert(entMan.EntityExists(entity));

            // Notice the recursion starts at the Owner of the passed in entity, this
            // allows containers inside containers (toolboxes in lockers).
            if (entMan.GetComponent<TransformComponent>(entity).ParentUid is not EntityUid { Valid: true} parent)
                return false;

            if (TryGetManagerComp(parent, out var containerComp, entMan))
                return containerComp.ContainsEntity(entity);

            return false;
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
        /// </summary>
        /// <param name="entity">Entity that might be inside a container.</param>
        /// <param name="force">Whether to forcibly remove the entity from the container.</param>
        /// <param name="wasInContainer">Whether the entity was actually inside a container or not.</param>
        /// <returns>If the entity could be removed. Also returns false if it wasn't inside a container.</returns>
        public static bool TryRemoveFromContainer(this EntityUid entity, bool force, out bool wasInContainer, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            DebugTools.Assert(entMan.EntityExists(entity));

            if (TryGetContainer(entity, out var container, entMan))
            {
                wasInContainer = true;

                if (!force)
                    return container.Remove(entity, entMan);

                container.ForceRemove(entity, entMan);
                return true;
            }

            wasInContainer = false;
            return false;
        }

        /// <summary>
        /// Attempts to remove an entity from its container, if any.
        /// </summary>
        /// <param name="entity">Entity that might be inside a container.</param>
        /// <param name="force">Whether to forcibly remove the entity from the container.</param>
        /// <returns>If the entity could be removed. Also returns false if it wasn't inside a container.</returns>
        public static bool TryRemoveFromContainer(this EntityUid entity, bool force = false, IEntityManager? entMan = null)
        {
            return TryRemoveFromContainer(entity, force, out _, entMan);
        }

        /// <summary>
        /// Attempts to remove all entities in a container.
        /// </summary>
        public static void EmptyContainer(this IContainer container, bool force = false, EntityCoordinates? moveTo = null,
            bool attachToGridOrMap = false, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            foreach (var entity in container.ContainedEntities.ToArray())
            {
                if (entMan.Deleted(entity))
                    continue;

                if (force)
                    container.ForceRemove(entity, entMan);
                else
                    container.Remove(entity, entMan);

                if (moveTo.HasValue)
                    entMan.GetComponent<TransformComponent>(entity).Coordinates = moveTo.Value;

                if(attachToGridOrMap)
                    entMan.GetComponent<TransformComponent>(entity).AttachToGridOrMap();
            }
        }

        /// <summary>
        /// Attempts to remove and delete all entities in a container.
        /// </summary>
        public static void CleanContainer(this IContainer container, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            foreach (var ent in container.ContainedEntities.ToArray())
            {
                if (entMan.Deleted(ent)) continue;
                container.ForceRemove(ent, entMan);
                entMan.DeleteEntity(ent);
            }
        }

        public static void AttachParentToContainerOrGrid(this TransformComponent transform, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            if (transform.Parent == null
                || !TryGetContainer(transform.Parent.Owner, out var container, entMan)
                || !TryInsertIntoContainer(transform, container, entMan))
                transform.AttachToGridOrMap();
        }

        private static bool TryInsertIntoContainer(this TransformComponent transform, IContainer container, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            if (container.Insert(transform.Owner, entMan)) return true;

            if (entMan.GetComponent<TransformComponent>(container.Owner).Parent != null
                && TryGetContainer(container.Owner, out var newContainer, entMan))
                return TryInsertIntoContainer(transform, newContainer, entMan);

            return false;
        }

        private static bool TryGetManagerComp(this EntityUid entity, [NotNullWhen(true)] out IContainerManager? manager, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            DebugTools.Assert(entMan.EntityExists(entity));

            if (entMan.TryGetComponent(entity, out manager))
                return true;

            // RECURSION ALERT
            if (entMan.GetComponent<TransformComponent>(entity).Parent != null)
                return TryGetManagerComp(entMan.GetComponent<TransformComponent>(entity).ParentUid, out manager, entMan);

            return false;
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
        public static T CreateContainer<T>(this EntityUid entity, string containerId, IEntityManager? entMan = null)
            where T : IContainer
        {
            IoCManager.Resolve(ref entMan);
            var containermanager = entMan.EnsureComponent<ContainerManagerComponent>(entity);
            return containermanager.MakeContainer<T>(containerId);
        }

        public static T EnsureContainer<T>(this EntityUid entity, string containerId, IEntityManager? entMan = null)
            where T : IContainer
        {
            IoCManager.Resolve(ref entMan);
            return EnsureContainer<T>(entity, containerId, out _, entMan);
        }

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
