using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
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
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsInContainer(IEntity entity)
        {
            DebugTools.Assert(entity != null);
            DebugTools.Assert(!entity.Deleted);

            //TODO: This code needs to check if the entity is actually inside one of the containerManager's containers.
            // We make the assumption that if a parent entity has a containerManager, the child is inside
            // one of the containers. Notice the recursion starts at the Owner of the passed in entity, this
            // allows containers inside containers (toolboxes in lockers).
            if (entity.Transform.Parent != null)
                return TryGetManagerComp(entity.Transform.Parent.Owner, out _);

            return false;
        }

        private static bool TryGetManagerComp(IEntity entity, out IContainerManager manager)
        {
            DebugTools.Assert(entity != null);
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
