using Robust.Shared.Physics.Broadphase;

namespace Robust.Client.Physics
{
    internal sealed class BroadPhaseSystem : SharedBroadPhaseSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(PhysicsSystem));
        }
    }
}
