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
        public IBroadPhase DynamicTree = default!;

        /// <summary>
        /// Stores all static bodies.
        /// </summary>
        public IBroadPhase StaticTree = default!;

        /// <summary>
        /// Stores all other non-static entities not in another tree.
        /// </summary>
        public DynamicTree<EntityUid> SundriesTree = default!;

        /// <summary>
        /// Stores all other static entities not in another tree.
        /// </summary>
        public DynamicTree<EntityUid> StaticSundriesTree = default!;
    }
}
