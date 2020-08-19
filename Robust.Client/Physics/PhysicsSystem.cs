using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Robust.Client.Physics
{
    [UsedImplicitly]
    public class PhysicsSystem : SharedPhysicsSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;

        private TimeSpan _lastRem;

        public override void Update(float frameTime)
        {
            _lastRem = _gameTiming.CurTime;

            SimulateWorld(frameTime, EntityManager.ComponentManager.EntityQuery<ICollidableComponent>().ToList(), !_gameTiming.InSimulation || !_gameTiming.IsFirstTimePredicted);
        }

        public override void FrameUpdate(float frameTime)
        {
            if (_lastRem > _gameTiming.TickRemainder)
            {
                _lastRem = TimeSpan.Zero;
            }

            var diff = _gameTiming.TickRemainder - _lastRem;
            _lastRem = _gameTiming.TickRemainder;
            SimulateWorld((float) diff.TotalSeconds, ActuallyRelevant(), true);
        }

        private List<ICollidableComponent> ActuallyRelevant()
        {
            var relevant = _componentManager.EntityQuery<ICollidableComponent>().Where(p => p.Predict)
                .ToList();
            return relevant;
        }
    }
}
