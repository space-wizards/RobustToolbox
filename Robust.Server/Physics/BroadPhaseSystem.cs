using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Robust.Server.Physics
{
    internal sealed partial class BroadPhaseSystem : SharedBroadphaseSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(PhysicsSystem));
        }
    }
}
