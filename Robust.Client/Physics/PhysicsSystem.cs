using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
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

        /// <summary>
        /// When substepping the client needs to know about the first position to use for lerping.
        /// </summary>
        private readonly Dictionary<EntityUid, (EntityUid ParentUid, Vector2 LocalPosition, Angle LocalRotation)>
            _lerpData = new();

        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, _gameTiming.InPrediction);
        }

        protected override void Cleanup(float frameTime)
        {
            var toRemove = new List<PhysicsComponent>();

            // Because we're not predicting 99% of bodies its sleep timer never gets incremented so we'll just do it ourselves.
            // (and serializing it over the network isn't necessary?)
            // This is a client-only problem.
            // Also need to suss out having the client build the island anyway and just... not solving it?
            var awakeQuery = AllEntityQuery<AwakePhysicsComponent, PhysicsComponent>();

            while (awakeQuery.MoveNext(out _, out var body))
            {
                if (body.LinearVelocity.Length > LinearToleranceSqr / 2f || body.AngularVelocity * body.AngularVelocity > AngularToleranceSqr / 2f) continue;
                body.SleepTime += frameTime;
                if (body.SleepTime > TimeToSleep)
                {
                    toRemove.Add(body);
                }
            }

            foreach (var body in toRemove)
            {
                SetAwake(body, false);
            }

            base.Cleanup(frameTime);
        }

        protected override void UpdateLerpData()
        {
            var awakeQuery = AllEntityQuery<AwakePhysicsComponent, PhysicsComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();

            while (awakeQuery.MoveNext(out _, out var body))
            {
                if (body.BodyType == BodyType.Static ||
                    _lerpData.TryGetValue(body.Owner, out var lerpData) ||
                    !xformQuery.TryGetComponent(body.Owner, out var xform) ||
                    lerpData.ParentUid == xform.ParentUid)
                {
                    continue;
                }

                _lerpData[xform.Owner] = (xform.ParentUid, xform.LocalPosition, xform.LocalRotation);
            }
        }

        /// <summary>
        /// Flush all of our lerping data.
        /// </summary>
        protected override void FinalStep()
        {
            base.FinalStep();
            var xformQuery = GetEntityQuery<TransformComponent>();

            foreach (var (uid, (parentUid, position, rotation)) in _lerpData)
            {
                if (!xformQuery.TryGetComponent(uid, out var xform) ||
                    !parentUid.IsValid())
                {
                    continue;
                }

                xform.PrevPosition = position;
                xform.PrevRotation = rotation;
                xform.LerpParent = parentUid;
                xform.NextPosition = xform.LocalPosition;
                xform.NextRotation = xform.LocalRotation;
            }

            _lerpData.Clear();
        }
    }
}
