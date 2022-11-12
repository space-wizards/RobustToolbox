using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Robust.Client.Physics
{
    [UsedImplicitly]
    public sealed class PhysicsSystem : SharedPhysicsSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, _gameTiming.InPrediction);
        }
    }
}
