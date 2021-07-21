using Robust.Shared.GameObjects;

namespace Robust.Shared.Physics
{
    [RegisterComponent]
    public sealed class BroadphaseComponent : Component
    {
        public override string Name => "Broadphase";

        internal IBroadPhase Tree = default!;
    }
}
