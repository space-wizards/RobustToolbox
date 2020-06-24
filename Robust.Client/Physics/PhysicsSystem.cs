using System;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Robust.Client.Physics
{
    [UsedImplicitly]
    public class PhysicsSystem : SharedPhysicsSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private TimeSpan _lastRem;

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery<PhysicsComponent>();
        }

        public override void Update(float frameTime)
        {
            _lastRem = _gameTiming.CurTime;
            SimulateWorld(frameTime,
                RelevantEntities.Where(e => !e.Deleted && e.GetComponent<PhysicsComponent>().Predict).ToList());
        }

        public override void FrameUpdate(float frameTime)
        {
            if (_lastRem > _gameTiming.TickRemainder)
            {
                _lastRem = TimeSpan.Zero;
            }

            var diff = _gameTiming.TickRemainder - _lastRem;
            _lastRem = _gameTiming.TickRemainder;
            SimulateWorld((float) diff.TotalSeconds, RelevantEntities.Where(e => !e.Deleted && e.GetComponent<PhysicsComponent>().Predict).ToList());
        }
    }
}
