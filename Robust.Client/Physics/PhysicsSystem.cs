using System;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Robust.Client.Physics
{
    [UsedImplicitly]
    public class PhysicsSystem : SharedPhysicsSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private TimeSpan _lastRem;

        public override void Update(float frameTime)
        {
            _lastRem = _gameTiming.CurTime;

            var predicted = !_gameTiming.InSimulation || !_gameTiming.IsFirstTimePredicted;
            SimulateWorld(frameTime, predicted);

            if (predicted)
                PostPhysics();
        }

        private void PostPhysics()
        {
            var player = _playerManager.LocalPlayer?.ControlledEntity;

            if (player == null || !player.TryGetComponent(out IPhysBody? physicsComponent)) return;

            // tl;dr anything we've collided with mark as predicted to avoid the JANK
            foreach (var collision in CollisionCache)
            {
                if (collision.A == physicsComponent)
                {
                    collision.B.Predict = true;
                }
                else if (collision.B == physicsComponent)
                {
                    collision.A.Predict = true;
                }
            }
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
            PostPhysics();
        }
    }
}
