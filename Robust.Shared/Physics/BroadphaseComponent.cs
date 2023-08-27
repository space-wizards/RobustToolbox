using Robust.Shared.GameObjects;
using Robust.Shared.Physics.BroadPhase;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// Stores the broadphase structure for the relevant grid / map.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BroadphaseComponent : Component
    {
        /// <summary>
        /// Stores all non-static bodies.
        /// </summary>
        public IBroadPhase DynamicTree = new DynamicTreeBroadPhase();

        /// <summary>
        /// Stores all static bodies.
        /// </summary>
        public IBroadPhase StaticTree = new DynamicTreeBroadPhase();

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
