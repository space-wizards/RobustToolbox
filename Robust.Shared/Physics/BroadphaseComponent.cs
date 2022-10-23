using Robust.Shared.GameObjects;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// Stores the broadphase structure for the relevant grid / map.
    /// </summary>
    [RegisterComponent]
    public sealed class BroadphaseComponent : Component
    {
        /// <summary>
        /// Stores all non-static bodies.
        /// </summary>
        internal IBroadPhase DynamicTree = default!;

        /// <summary>
        /// Stores all static bodies.
        /// </summary>
        internal IBroadPhase StaticTree = default!;

        /// <summary>
        /// Stores all entities not in another tree.
        /// </summary>
        internal DynamicTree<EntityUid> SundriesTree = default!;
    }
}
