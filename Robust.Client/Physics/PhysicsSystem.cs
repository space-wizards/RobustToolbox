using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Robust.Client.Physics
{
    [UsedImplicitly]
    public sealed partial class PhysicsSystem : SharedPhysicsSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;

        public override void Update(float frameTime)
        {
            UpdateIsPredicted();
            SimulateWorld(frameTime, _gameTiming.InPrediction);
        }

        protected override void Cleanup(float frameTime)
        {
            var toRemove = new ValueList<Entity<PhysicsComponent, TransformComponent>>();

            // Because we're not predicting 99% of bodies its sleep timer never gets incremented so we'll just do it ourselves.
            // (and serializing it over the network isn't necessary?)
            // This is a client-only problem.
            // Also need to suss out having the client build the island anyway and just... not solving it?
            foreach (var ent in AwakeBodies)
            {
                var body = ent.Comp1;

                if (!body.SleepingAllowed || body.LinearVelocity.Length() > LinearToleranceSqr / 2f || body.AngularVelocity * body.AngularVelocity > AngularToleranceSqr / 2f) continue;
                body.SleepTime += frameTime;
                if (body.SleepTime > TimeToSleep)
                {
                    toRemove.Add(ent);
                }
            }

            foreach (var body in toRemove)
            {
                SetAwake(body, false);
            }

            base.Cleanup(frameTime);
        }

        protected override void UpdateLerpData(List<Entity<PhysicsComponent, TransformComponent>> bodies)
        {
            foreach (var bodyEnt in bodies)
            {
                var body = bodyEnt.Comp1;
                var xform = bodyEnt.Comp2;

                if (body.BodyType == BodyType.Static ||
                    LerpData.TryGetValue(bodyEnt, out var lerpData) ||
                    lerpData == xform.ParentUid)
                {
                    continue;
                }

                LerpData[bodyEnt.Owner] = xform.ParentUid;
            }
        }

        /// <summary>
        /// Flush all of our lerping data.
        /// </summary>
        protected override void FinalStep()
        {
            base.FinalStep();

            foreach (var (uid, parentUid) in LerpData)
            {
                // Can't just re-use xform from before as movement events may cause event subs to fire.
                if (!XformQuery.TryGetComponent(uid, out var xform) || !parentUid.IsValid())
                {
                    continue;
                }

                // Transform system will handle lerping.
                _transform.SetLocalPositionRotation(uid, xform.LocalPosition, xform.LocalRotation, xform);
            }

            LerpData.Clear();
        }
    }
}
