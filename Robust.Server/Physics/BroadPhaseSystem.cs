using Robust.Server.GameObjects;
using Robust.Shared.Physics;

namespace Robust.Server.Physics
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
