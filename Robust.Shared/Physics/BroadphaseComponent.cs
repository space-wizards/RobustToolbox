using Robust.Shared.GameObjects;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// Stores the broadphase structure for the relevant grid / map.
    /// </summary>
    [RegisterComponent]
    public sealed class BroadphaseComponent : Component
    {
        internal IBroadPhase Tree = default!;
    }
}
