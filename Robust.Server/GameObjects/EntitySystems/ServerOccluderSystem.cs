using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Physics;

namespace Robust.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class ServerOccluderSystem : OccluderSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(SharedPhysicsSystem));
        }
    }
}
