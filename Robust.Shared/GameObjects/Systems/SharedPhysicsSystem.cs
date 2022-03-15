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
            SubscribeLocalEvent<MapChangedEvent>(ev =>
            {
                if (ev.Created)
                    HandleMapCreated(ev);
            });

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
            configManager.OnValueChanged(CVars.AutoClearForces, OnAutoClearChange);
        }

        private void HandlePhysicsMapInit(EntityUid uid, SharedPhysicsMapComponent component, ComponentInit args)
        {
            IoCManager.InjectDependencies(component);
            component.BroadphaseSystem = _broadphaseSystem;
            component._physics = this;
            component.ContactManager = new();
            component.ContactManager.Initialize();
            component.ContactManager.MapId = component.MapId;
            component.AutoClearForces = IoCManager.Resolve<IConfigurationManager>().GetCVar(CVars.AutoClearForces);

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
            if (LifeStage(uid) is < EntityLifeStage.Initialized or > EntityLifeStage.MapInitialized
                || !TryComp(uid, out TransformComponent? xform))
            {
                return;
            }
                
            if (body.CanCollide)
                _broadphase.UpdateBroadphase(body, xform: xform);

            HandleParentChangeVelocity(uid, body, ref args, xform);
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

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.UnsubValueChanged(CVars.AutoClearForces, OnAutoClearChange);
        }

        protected abstract void HandleMapCreated(MapChangedEvent eventArgs);

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
            
            _physicsManager.ClearTransforms();
        }

        internal static (int Batches, int BatchSize) GetBatch(int count, int minimumBatchSize)
        {
            var batches = Math.Min(
                (int) MathF.Ceiling((float) count / minimumBatchSize),
                Math.Max(1, Environment.ProcessorCount));
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
