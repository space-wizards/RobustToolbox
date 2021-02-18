using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    [UsedImplicitly]
    public sealed class ServerOccluderSystem : OccluderSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(PhysicsSystem));
        }
    }
}
