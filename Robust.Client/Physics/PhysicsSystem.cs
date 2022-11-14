using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
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

        protected override void Cleanup(SharedPhysicsMapComponent component, float frameTime)
        {
            var toRemove = new List<PhysicsComponent>();

            // Because we're not predicting 99% of bodies its sleep timer never gets incremented so we'll just do it ourselves.
            // (and serializing it over the network isn't necessary?)
            // This is a client-only problem.
            // Also need to suss out having the client build the island anyway and just... not solving it?
            foreach (var body in component.AwakeBodies)
            {
                if (body.LinearVelocity.Length > _linearToleranceSqr / 2f || body.AngularVelocity * body.AngularVelocity > _angularToleranceSqr / 2f) continue;
                body.SleepTime += frameTime;
                if (body.SleepTime > _timeToSleep)
                {
                    toRemove.Add(body);
                }
            }

            foreach (var body in toRemove)
            {
                body.Awake = false;
            }

            base.Cleanup(frameTime);
        }
    }
}
