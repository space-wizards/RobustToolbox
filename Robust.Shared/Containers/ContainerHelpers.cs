using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Helper functions for the container system.
    /// </summary>
    public static class ContainerHelpers
    {
        /// <summary>
        /// Am I inside a container?
        /// </summary>
        /// <param name="entity">Entity that might be inside a container.</param>
        /// <returns>If the entity is inside of a container.</returns>
        public static bool IsInContainer(IEntity entity)
        {
            DebugTools.AssertNotNull(entity);
            DebugTools.Assert(!entity.Deleted);

            // Notice the recursion starts at the Owner of the passed in entity, this
            // allows containers inside containers (toolboxes in lockers).
            if (entity.Transform.Parent != null)
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
        public static bool TryGetContainerMan(IEntity entity, [NotNullWhen(true)] out IContainerManager? manager)
        {
            DebugTools.AssertNotNull(entity);
            DebugTools.Assert(!entity.Deleted);

            if (entity.Transform.Parent != null && TryGetManagerComp(entity.Transform.Parent.Owner, out manager) && manager.ContainsEntity(entity))
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
        public static bool TryGetContainer(IEntity entity, [NotNullWhen(true)] out IContainer? container)
        {
            DebugTools.AssertNotNull(entity);
            DebugTools.Assert(!entity.Deleted);

            if (TryGetContainerMan(entity, out var manager))
                return manager.TryGetContainer(entity, out container);

            container = default;
            return false;
        }

        private static bool TryGetManagerComp(IEntity entity, [NotNullWhen(true)] out IContainerManager? manager)
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
    }
}
