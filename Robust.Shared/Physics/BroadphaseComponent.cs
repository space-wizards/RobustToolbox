using Robust.Shared.GameObjects;

namespace Robust.Shared.Physics
{
    [RegisterComponent]
    public sealed class BroadphaseComponent : Component
    {
        internal IBroadPhase Tree = default!;
    }
}
