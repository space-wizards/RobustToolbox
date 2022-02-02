using System;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Timing;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Shared.GameObjects
{
    public abstract partial class SharedPhysicsSystem : EntitySystem
    {
        /*
         * TODO:

         * Raycasts for non-box shapes.
         * SetTransformIgnoreContacts for teleports (and anything else left on the physics body in Farseer)
         * TOI Solver (continuous collision detection)
         * Poly cutting
         * Chain shape
         * A bunch of objects have collision on round start
         */

        public static readonly Histogram TickUsageControllerBeforeSolveHistogram = Metrics.CreateHistogram("robust_entity_physics_controller_before_solve",
            "Amount of time spent running a controller's UpdateBeforeSolve", new HistogramConfiguration
            {
                LabelNames = new[] {"controller"},
                Buckets = Histogram.ExponentialBuckets(0.000_001, 1.5, 25)
            });

        public static readonly Histogram TickUsageControllerAfterSolveHistogram = Metrics.CreateHistogram("robust_entity_physics_controller_after_solve",
            "Amount of time spent running a controller's UpdateAfterSolve", new HistogramConfiguration
            {
                LabelNames = new[] {"controller"},
                Buckets = Histogram.ExponentialBuckets(0.000_001, 1.5, 25)
            });

        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedJointSystem _joints = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        public Action<Fixture, Fixture, float, Vector2>? KinematicControllerCollision;

        public bool MetricsEnabled { get; protected set; }
        private readonly Stopwatch _stopwatch = new();

        public override void Initialize()
        {
            base.Initialize();
            MapManager.MapCreated += HandleMapCreated;

            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInit);
            SubscribeLocalEvent<CollisionChangeMessage>(HandlePhysicsUpdateMessage);
            SubscribeLocalEvent<PhysicsWakeMessage>(HandleWakeMessage);
            SubscribeLocalEvent<PhysicsSleepMessage>(HandleSleepMessage);
            SubscribeLocalEvent<EntMapIdChangedMessage>(HandleMapChange);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerInserted);
            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleContainerRemoved);
            SubscribeLocalEvent<PhysicsComponent, EntParentChangedMessage>(HandleParentChange);
            SubscribeLocalEvent<SharedPhysicsMapComponent, ComponentInit>(HandlePhysicsMapInit);
            SubscribeLocalEvent<SharedPhysicsMapComponent, ComponentRemove>(HandlePhysicsMapRemove);
            SubscribeLocalEvent<PhysicsComponent, ComponentInit>(OnPhysicsInit);

            IoCManager.Resolve<IIslandManager>().Initialize();

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.AutoClearForces, OnAutoClearChange, true);
        }

        private void HandlePhysicsMapInit(EntityUid uid, SharedPhysicsMapComponent component, ComponentInit args)
        {
            IoCManager.InjectDependencies(component);
            component.BroadphaseSystem = _broadphaseSystem;
            component._physics = this;
            component.ContactManager = new();
            component.ContactManager.Initialize();
            component.ContactManager.MapId = component.MapId;

            component.ContactManager.KinematicControllerCollision += KinematicControllerCollision;
        }

        private void OnAutoClearChange(bool value)
        {
            foreach (var component in EntityManager.EntityQuery<SharedPhysicsMapComponent>(true))
            {
                component.AutoClearForces = value;
            }
        }

        private void HandlePhysicsMapRemove(EntityUid uid, SharedPhysicsMapComponent component, ComponentRemove args)
        {
            component.ContactManager.KinematicControllerCollision -= KinematicControllerCollision;
            component.ContactManager.Shutdown();
        }

        private void HandleParentChange(EntityUid uid, PhysicsComponent body, ref EntParentChangedMessage args)
        {
            if (LifeStage(uid) != EntityLifeStage.MapInitialized || !TryComp(uid, out TransformComponent? xform))
                return;

            if (body.CanCollide)
                _broadphase.UpdateBroadphase(body, xform: xform);
            
            if (!xform.ParentUid.IsValid() || !_container.IsEntityInContainer(uid, xform))
                HandleParentChangeVelocity(uid, body, ref args, xform);
        }

        private void HandleParentChangeVelocity(EntityUid uid, PhysicsComponent body, ref EntParentChangedMessage args, TransformComponent xform)
        {
            var angularVelocityDiff = 0f;
            var linearVelocityDiff = Vector2.Zero;

            var (worldPos, worldRot) = xform.GetWorldPositionRotation();
            var R = Matrix3.CreateRotation(worldRot);
            R.Transpose(out var nRT);
            nRT.Multiply(-1f);

            if (args.OldParent is {Valid: true} oldParent && EntityManager.TryGetComponent(oldParent, out PhysicsComponent? oldBody))
            {
                var (linear, angular) = oldBody.MapVelocities;

                // Our rotation system is backwards; invert the angular velocity
                var o = -angular;

                // Get the vector between the parent and the entity leaving
                var r = Transform(oldParent).WorldMatrix.Transform(oldBody.LocalCenter) -
                    worldPos; // TODO: Use entity's LocalCenter/center of mass somehow

                // Get the tangent of r by rotating it π/2 rad (90°)
                var v = new Angle(MathHelper.PiOver2).RotateVec(r);

                // Scale the new vector by the angular velocity
                v *= o;

                // Makes the shit spin right when dropped near the center of mass of a spinning body.
                // I have no clue how it works, but this guy does/did:
                //
                // https://cwzx.wordpress.com/2014/03/25/the-dynamics-of-a-transform-hierarchy/
                var w = new Matrix3(
                    0, angular, 0,
                    -angular, 0, 0,
                    0, 0, 0
                );

                linearVelocityDiff += linear + v;
                angularVelocityDiff += (nRT * w * R).R1C0;
            }

            var newParent = xform.ParentUid;

            if (newParent != EntityUid.Invalid && EntityManager.TryGetComponent(newParent, out PhysicsComponent? newBody))
            {
                // TODO: Apply child's linear as angular on the parent?
                linearVelocityDiff -= newBody.MapLinearVelocity;

                // See above for commentary.
                var angular = newBody.AngularVelocity;
                var o = -angular;
                var r = Transform(newParent).WorldMatrix.Transform(newBody.LocalCenter) - worldPos;
                var v = new Angle(MathHelper.PiOver2).RotateVec(r);
                v *= o;

                var w = new Matrix3(
                    0, angular, 0,
                    -angular, 0, 0,
                    0, 0, 0
                );

                angularVelocityDiff -= (nRT * w * R).R1C0;
            }

            body.LinearVelocity += linearVelocityDiff;
            body.AngularVelocity += angularVelocityDiff;
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            if (!EntityManager.EntityExists(ev.EntityUid)) return;
            // Yes this ordering matters
            var collideComp = EntityManager.EnsureComponent<PhysicsComponent>(ev.EntityUid);
            collideComp.BodyType = BodyType.Static;
            EntityManager.EnsureComponent<FixturesComponent>(ev.EntityUid);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            MapManager.MapCreated -= HandleMapCreated;

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.UnsubValueChanged(CVars.AutoClearForces, OnAutoClearChange);
        }

        protected abstract void HandleMapCreated(object? sender, MapEventArgs eventArgs);

        private void HandleMapChange(EntMapIdChangedMessage message)
        {
            if (!EntityManager.TryGetComponent(message.Entity, out PhysicsComponent? physicsComponent))
                return;

            _joints.ClearJoints(physicsComponent);

            // So if the map is being deleted it detaches all of its bodies to null soooo we have this fun check.

            var oldMapId = message.OldMapId;
            if (oldMapId != MapId.Nullspace)
            {
                var oldMapEnt = MapManager.GetMapEntityId(oldMapId);

                if (MetaData(oldMapEnt).EntityLifeStage < EntityLifeStage.Terminating)
                {
                    EntityManager.GetComponent<SharedPhysicsMapComponent>(oldMapEnt).RemoveBody(physicsComponent);
                }
            }

            var newMapId = Transform(message.Entity).MapID;

            if (newMapId != MapId.Nullspace)
            {
                EntityManager.GetComponent<SharedPhysicsMapComponent>(MapManager.GetMapEntityId(newMapId)).AddBody(physicsComponent);
            }
        }

        private void HandlePhysicsUpdateMessage(CollisionChangeMessage message)
        {
            var mapId = EntityManager.GetComponent<TransformComponent>(message.Owner).MapID;

            if (mapId == MapId.Nullspace)
                return;

            var physicsMap = EntityManager.GetComponent<SharedPhysicsMapComponent>(MapManager.GetMapEntityId(mapId));

            if (Deleted(message.Owner) || !message.CanCollide)
            {
                physicsMap.RemoveBody(message.Body);
            }
            else
            {
                physicsMap.AddBody(message.Body);
            }
        }

        private void HandleWakeMessage(PhysicsWakeMessage message)
        {
            var mapId = EntityManager.GetComponent<TransformComponent>(message.Body.Owner).MapID;

            if (mapId == MapId.Nullspace)
                return;

            EntityUid tempQualifier = MapManager.GetMapEntityId(mapId);
            EntityManager.GetComponent<SharedPhysicsMapComponent>(tempQualifier).AddAwakeBody(message.Body);
        }

        private void HandleSleepMessage(PhysicsSleepMessage message)
        {
            var mapId = EntityManager.GetComponent<TransformComponent>(message.Body.Owner).MapID;

            if (mapId == MapId.Nullspace)
                return;

            EntityUid tempQualifier = MapManager.GetMapEntityId(mapId);
            EntityManager.GetComponent<SharedPhysicsMapComponent>(tempQualifier).RemoveSleepBody(message.Body);
        }

        private void HandleContainerInserted(EntInsertedIntoContainerMessage message)
        {
            if (!EntityManager.TryGetComponent(message.Entity, out PhysicsComponent? physicsComponent)) return;

            var mapId = EntityManager.GetComponent<TransformComponent>(message.Container.Owner).MapID;

            physicsComponent.LinearVelocity = Vector2.Zero;
            physicsComponent.AngularVelocity = 0.0f;
            _joints.ClearJoints(physicsComponent);

            if (mapId != MapId.Nullspace)
            {
                EntityUid tempQualifier = MapManager.GetMapEntityId(mapId);
                EntityManager.GetComponent<SharedPhysicsMapComponent>(tempQualifier).RemoveBody(physicsComponent);
            }
        }

        private void HandleContainerRemoved(EntRemovedFromContainerMessage message)
        {
            if (!EntityManager.TryGetComponent(message.Entity, out PhysicsComponent? physicsComponent)) return;

            var mapId = EntityManager.GetComponent<TransformComponent>(message.Container.Owner).MapID;

            if (mapId != MapId.Nullspace)
            {
                EntityUid tempQualifier = MapManager.GetMapEntityId(mapId);
                EntityManager.GetComponent<SharedPhysicsMapComponent>(tempQualifier).AddBody(physicsComponent);
            }
        }

        /// <summary>
        ///     Simulates the physical world for a given amount of time.
        /// </summary>
        /// <param name="deltaTime">Delta Time in seconds of how long to simulate the world.</param>
        /// <param name="prediction">Should only predicted entities be considered in this simulation step?</param>
        protected void SimulateWorld(float deltaTime, bool prediction)
        {
            var updateBeforeSolve = new PhysicsUpdateBeforeSolveEvent(prediction, deltaTime);
            RaiseLocalEvent(ref updateBeforeSolve);

            foreach (var comp in EntityManager.EntityQuery<SharedPhysicsMapComponent>(true))
            {
                comp.Step(deltaTime, prediction);
            }

            var updateAfterSolve = new PhysicsUpdateAfterSolveEvent(prediction, deltaTime);
            RaiseLocalEvent(ref updateAfterSolve);

            // Go through and run all of the deferred events now
            foreach (var comp in EntityManager.EntityQuery<SharedPhysicsMapComponent>(true))
            {
                comp.ProcessQueue();
            }

            _broadphaseSystem.Cleanup();
            _physicsManager.ClearTransforms();
        }

        internal static (int Batches, int BatchSize) GetBatch(int count, int minimumBatchSize)
        {
            var batches = Math.Min((int) MathF.Floor((float) count / minimumBatchSize), Math.Max(1, Environment.ProcessorCount));
            var batchSize = (int) MathF.Ceiling((float) count / batches);

            return (batches, batchSize);
        }
    }

    [ByRefEvent]
    public readonly struct PhysicsUpdateAfterSolveEvent
    {
        public readonly bool Prediction;
        public readonly float DeltaTime;

        public PhysicsUpdateAfterSolveEvent(bool prediction, float deltaTime)
        {
            Prediction = prediction;
            DeltaTime = deltaTime;
        }
    }

    [ByRefEvent]
    public readonly struct PhysicsUpdateBeforeSolveEvent
    {
        public readonly bool Prediction;
        public readonly float DeltaTime;

        public PhysicsUpdateBeforeSolveEvent(bool prediction, float deltaTime)
        {
            Prediction = prediction;
            DeltaTime = deltaTime;
        }
    }
}
