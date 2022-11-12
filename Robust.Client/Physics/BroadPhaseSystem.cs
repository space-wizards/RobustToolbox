using Robust.Shared.Physics.Systems;

namespace Robust.Client.Physics
{
    internal sealed class BroadPhaseSystem : SharedBroadphaseSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(PhysicsSystem));
        }
    }
}
