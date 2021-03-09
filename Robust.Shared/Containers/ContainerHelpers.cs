using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
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
        public static bool IsInContainer(this IEntity entity)
        {
            DebugTools.AssertNotNull(entity);
            DebugTools.Assert(!entity.Deleted);

            // Notice the recursion starts at the Owner of the passed in entity, this
            // allows containers inside containers (toolboxes in lockers).
            if (entity.Transform.Parent == null)
                return false;

            if (TryGetManagerComp(entity.Transform.Parent.Owner, out var containerComp))
                return containerComp.ContainsEntity(entity);

            return false;
        }

        /// <summary>
        /// Tries to find the container manager that this entity is inside (if any).
        /// </summary>
        /// <param name="entity">Entity that might be inside a container.</param>
        /// <param name="manager">The container manager that this entity is inside of.</param>
        /// <returns>If a container manager was found.</returns>
        public static bool TryGetContainerMan(this IEntity entity, [NotNullWhen(true)] out IContainerManager? manager)
        {
            DebugTools.AssertNotNull(entity);
            DebugTools.Assert(!entity.Deleted);

            var parentTransform = entity.Transform.Parent;
            if (parentTransform != null && TryGetManagerComp(parentTransform.Owner, out manager) && manager.ContainsEntity(entity))
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
        public static bool TryGetContainer(this IEntity entity, [NotNullWhen(true)] out IContainer? container)
        {
            DebugTools.AssertNotNull(entity);
            DebugTools.Assert(!entity.Deleted);

            if (TryGetContainerMan(entity, out var manager))
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
        public static bool TryRemoveFromContainer(this IEntity entity, bool force, out bool wasInContainer)
        {
            DebugTools.AssertNotNull(entity);
            DebugTools.Assert(!entity.Deleted);

            if (TryGetContainer(entity, out var container))
            {
                wasInContainer = true;

                if (!force)
                    return container.Remove(entity);

                container.ForceRemove(entity);
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
        public static bool TryRemoveFromContainer(this IEntity entity, bool force = false)
        {
            return TryRemoveFromContainer(entity, force, out _);
        }

        /// <summary>
        /// Attempts to remove all entities in a container.
        /// </summary>
        public static void EmptyContainer(this IContainer container, bool force = false, EntityCoordinates? moveTo = null, bool attachToGridOrMap = false)
        {
            foreach (var entity in container.ContainedEntities.ToArray())
            {
                if (entity.Deleted) continue;

                if (force)
                    container.ForceRemove(entity);
                else
                    container.Remove(entity);

                if (moveTo.HasValue)
                    entity.Transform.Coordinates = moveTo.Value;

                if(attachToGridOrMap)
                    entity.Transform.AttachToGridOrMap();
            }
        }

        /// <summary>
        /// Attempts to remove and delete all entities in a container.
        /// </summary>
        public static void CleanContainer(this IContainer container)
        {
            foreach (var ent in container.ContainedEntities.ToArray())
            {
                if (ent.Deleted) continue;
                container.ForceRemove(ent);
                ent.Delete();
            }
        }

        public static void AttachParentToContainerOrGrid(this ITransformComponent transform)
        {
            if (transform.Parent == null
                || !TryGetContainer(transform.Parent.Owner, out var container)
                || !TryInsertIntoContainer(transform, container))
                transform.AttachToGridOrMap();
        }

        private static bool TryInsertIntoContainer(this ITransformComponent transform, IContainer container)
        {
            if (container.Insert(transform.Owner)) return true;

            if (container.Owner.Transform.Parent != null
                && TryGetContainer(container.Owner, out var newContainer))
                return TryInsertIntoContainer(transform, newContainer);

            return false;
        }

        private static bool TryGetManagerComp(this IEntity entity, [NotNullWhen(true)] out IContainerManager? manager)
        {
            DebugTools.AssertNotNull(entity);
            DebugTools.Assert(!entity.Deleted);

            if (entity.TryGetComponent(out manager))
                return true;

            // RECURSION ALERT
            if (entity.Transform.Parent != null)
                return TryGetManagerComp(entity.Transform.Parent.Owner, out manager);

            return false;
        }

        public static bool IsInSameOrNoContainer(this IEntity user, IEntity other)
        {
            DebugTools.AssertNotNull(user);
            DebugTools.AssertNotNull(other);

            var isUserContained = TryGetContainer(user, out var userContainer);
            var isOtherContained = TryGetContainer(other, out var otherContainer);

            // Both entities are not in a container
            if (!isUserContained && !isOtherContained) return true;

            // Both entities are in different contained states
            if (isUserContained != isOtherContained) return false;

            // Both entities are in the same container
            return userContainer == otherContainer;
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
        public static T CreateContainer<T>(this IEntity entity, string containerId)
            where T : IContainer
        {
            if (!entity.TryGetComponent<IContainerManager>(out var containermanager))
                containermanager = entity.AddComponent<ContainerManagerComponent>();

            return containermanager.MakeContainer<T>(containerId);
        }

        public static T EnsureContainer<T>(this IEntity entity, string containerId)
            where T : IContainer
        {
            return EnsureContainer<T>(entity, containerId, out _);
        }

        public static T EnsureContainer<T>(this IEntity entity, string containerId, out bool alreadyExisted)
            where T : IContainer
        {
            var containerManager = entity.EnsureComponent<ContainerManagerComponent>();

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

        /// <summary>
        ///     Recursively retrieve all entities we contain.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="existing"></param>
        /// <param name="excluded"></param>
        /// <returns></returns>
        public static List<IEntity> GetContained(this IEntity entity, List<IEntity>? existing = null, HashSet<EntityUid>? excluded = null)
        {
            existing ??= new List<IEntity>();

            if (entity.Deleted || !entity.TryGetComponent(out IContainerManager? containerManager))
                return existing;

            var cast = (ContainerManagerComponent) containerManager;

            foreach (var container in cast.GetAllContainers())
            {
                foreach (var contained in container.ContainedEntities)
                {
                    if (contained == null || excluded?.Contains(contained.Uid) == true)
                        continue;

                    existing.Add(contained);
                    // Recursion time
                    GetContained(contained, existing, excluded);
                }
            }

            return existing;
        }
    }
}
