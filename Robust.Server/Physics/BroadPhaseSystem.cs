using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Physics.Broadphase;

namespace Robust.Server.Physics
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
