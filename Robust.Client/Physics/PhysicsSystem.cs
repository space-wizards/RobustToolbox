using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Robust.Client.Physics
{
    [UsedImplicitly]
    public class PhysicsSystem : SharedPhysicsSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private TimeSpan _lastRem;

        public override void Update(float frameTime)
        {
            _lastRem = _gameTiming.CurTime;
            SimulateWorld(frameTime, _gameTiming.InPrediction);
        }

        public override void FrameUpdate(float frameTime)
        {
            if (_lastRem > _gameTiming.TickRemainder)
            {
                _lastRem = TimeSpan.Zero;
            }

            var diff = _gameTiming.TickRemainder - _lastRem;
            _lastRem = _gameTiming.TickRemainder;
            SimulateWorld((float) diff.TotalSeconds, true);
        }

        protected override void HandleMapCreated(object? sender, MapEventArgs eventArgs)
        {
            if (eventArgs.Map == MapId.Nullspace) return;
            MapManager.GetMapEntity(eventArgs.Map).AddComponent<PhysicsMapComponent>();
        }
    }
}
