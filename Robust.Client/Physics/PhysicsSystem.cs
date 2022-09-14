using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
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

        protected override void OnMapAdded(ref MapChangedEvent eventArgs)
        {
            if (eventArgs.Map == MapId.Nullspace) return;
            EnsureComp<PhysicsMapComponent>(MapManager.GetMapEntityId(eventArgs.Map));
        }
    }
}
