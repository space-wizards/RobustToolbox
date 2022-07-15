using System;
using System.Linq;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;
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
        [Dependency] private readonly SharedJointSystem _joints = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedGridTraversalSystem _traversal = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        public Action<Fixture, Fixture, float, Vector2>? KinematicControllerCollision;

        public bool MetricsEnabled { get; protected set; }

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("physics");
            _sawmill.Level = LogLevel.Info;

            SubscribeLocalEvent<MapChangedEvent>(ev =>
            {
                if (ev.Created)
                    HandleMapCreated(ev);
            });

            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInit);
            SubscribeLocalEvent<PhysicsWakeEvent>(OnWake);
            SubscribeLocalEvent<PhysicsSleepEvent>(OnSleep);
            SubscribeLocalEvent<CollisionChangeEvent>(OnCollisionChange);
            SubscribeLocalEvent<PhysicsComponent, EntGotRemovedFromContainerMessage>(HandleContainerRemoved);
            SubscribeLocalEvent<PhysicsComponent, EntParentChangedMessage>(OnParentChange);
            SubscribeLocalEvent<SharedPhysicsMapComponent, ComponentInit>(HandlePhysicsMapInit);
            SubscribeLocalEvent<SharedPhysicsMapComponent, ComponentRemove>(HandlePhysicsMapRemove);
            SubscribeLocalEvent<PhysicsComponent, ComponentInit>(OnPhysicsInit);
            SubscribeLocalEvent<PhysicsComponent, ComponentRemove>(OnPhysicsRemove);
            SubscribeLocalEvent<PhysicsComponent, ComponentGetState>(OnPhysicsGetState);
            SubscribeLocalEvent<PhysicsComponent, ComponentHandleState>(OnPhysicsHandleState);

            IoCManager.Resolve<IIslandManager>().Initialize();

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.AutoClearForces, OnAutoClearChange);
        }

        private void OnPhysicsRemove(EntityUid uid, PhysicsComponent component, ComponentRemove args)
        {
            component.CanCollide = false;
            DebugTools.Assert(!component.Awake);
        }

        private void OnCollisionChange(ref CollisionChangeEvent ev)
        {
            var mapId = Transform(ev.Body.Owner).MapID;

            if (mapId == MapId.Nullspace)
                return;

            if (!ev.CanCollide)
            {
                DestroyContacts(ev.Body);
            }
        }

        private void HandlePhysicsMapInit(EntityUid uid, SharedPhysicsMapComponent component, ComponentInit args)
        {
            IoCManager.InjectDependencies(component);
            component.BroadphaseSystem = _broadphase;
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

        private void OnParentChange(EntityUid uid, PhysicsComponent body, ref EntParentChangedMessage args)
        {
            var meta = MetaData(uid);

            if (meta.EntityLifeStage < EntityLifeStage.Initialized)
                return;

            var xform = args.Transform;

            if ((meta.Flags & MetaDataFlags.InContainer) != 0)
            {
                // Here we intentionally dont dirty the physics comp. Client-side state handling will apply these same
                // changes. This also ensures that the server doesn't have to send the physics comp state to every
                // player for any entity inside of a container during init.
                SetLinearVelocity(body, Vector2.Zero, false);
                SetAngularVelocity(body, 0, false);
                SetCanCollide(body, false, false);
                _joints.ClearJoints(body);
            }

            // TODO: need to suss out this particular bit + containers + body.Broadphase.
            _broadphase.UpdateBroadphase(body, xform: xform);

            // Handle map change
            var mapId = _transform.GetMapId(args.Entity);

            if (args.OldMapId != mapId)
            {
                HandleMapChange(body, xform, args.OldMapId, mapId);
                _joints.ClearJoints(body);
            }

            if (body.BodyType != BodyType.Static && mapId != MapId.Nullspace && body._canCollide)
                HandleParentChangeVelocity(uid, body, ref args, xform);
        }

        private void HandleMapChange(PhysicsComponent body, TransformComponent xform, MapId oldMapId, MapId mapId)
        {
            // TODO: Could potentially migrate these but would need more thinking
            // For now just recursively destroy them
            RecursiveDestroyContacts(body, oldMapId);

            // Remove our old movebuffer
            _broadphase.RemoveFromMoveBuffer(body, oldMapId);

            _joints.ClearJoints(body);

            // So if the map is being deleted it detaches all of its bodies to null soooo we have this fun check.
            SharedPhysicsMapComponent? oldMap = null;
            SharedPhysicsMapComponent? map = null;

            // If the body isn't awake then nothing to do besides clearing joints.

            if (body.Awake)
            {
                if (oldMapId != MapId.Nullspace)
                {
                    var oldMapEnt = MapManager.GetMapEntityId(oldMapId);

                    if (TryComp(oldMapEnt, out oldMap))
                    {
                        oldMap.RemoveSleepBody(body);
                    }
                }

                if (mapId != MapId.Nullspace && TryComp(MapManager.GetMapEntityId(mapId), out map))
                {
                    map.AddAwakeBody(body);
                }

                DebugTools.Assert(body.Awake);
            }

            if (xform.ChildCount == 0 ||
                MapManager.IsMap(body.Owner)) return;

            var xformQuery = GetEntityQuery<TransformComponent>();
            var bodyQuery = GetEntityQuery<PhysicsComponent>();
            var metaQuery = GetEntityQuery<MetaDataComponent>();

            RecursiveMapUpdate(xform, oldMapId, oldMap, map, xformQuery, bodyQuery, metaQuery);
        }

        private void RecursiveMapUpdate(
            TransformComponent xform,
            MapId oldMapId,
            SharedPhysicsMapComponent? oldMap,
            SharedPhysicsMapComponent? map,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<PhysicsComponent> bodyQuery,
            EntityQuery<MetaDataComponent> metaQuery)
        {
            var childEnumerator = xform.ChildEnumerator;

            while (childEnumerator.MoveNext(out var child))
            {
                if (!bodyQuery.TryGetComponent(child.Value, out var childBody) ||
                    !xformQuery.TryGetComponent(child.Value, out var childXform) ||
                    metaQuery.GetComponent(child.Value).EntityLifeStage == EntityLifeStage.Deleted) continue;

                if (childBody.Awake)
                {
                    oldMap?.RemoveSleepBody(childBody);
                    map?.AddAwakeBody(childBody);
                }
                else
                {
                    DebugTools.Assert(oldMap?.AwakeBodies.Contains(childBody) != true);
                }

                _broadphase.RemoveFromMoveBuffer(childBody, oldMapId);
                DestroyContacts(childBody, oldMapId);
                _joints.ClearJoints(childBody);
                RecursiveMapUpdate(childXform, oldMapId, oldMap, map, xformQuery, bodyQuery, metaQuery);
            }
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

        private void OnWake(ref PhysicsWakeEvent @event)
        {
            var mapId = EntityManager.GetComponent<TransformComponent>(@event.Body.Owner).MapID;

            if (mapId == MapId.Nullspace)
                return;

            EntityUid tempQualifier = MapManager.GetMapEntityId(mapId);
            EntityManager.GetComponent<SharedPhysicsMapComponent>(tempQualifier).AddAwakeBody(@event.Body);
        }

        private void OnSleep(ref PhysicsSleepEvent @event)
        {
            var mapId = EntityManager.GetComponent<TransformComponent>(@event.Body.Owner).MapID;

            if (mapId == MapId.Nullspace)
                return;

            EntityUid tempQualifier = MapManager.GetMapEntityId(mapId);
            EntityManager.GetComponent<SharedPhysicsMapComponent>(tempQualifier).RemoveSleepBody(@event.Body);
        }

        private void HandleContainerRemoved(EntityUid uid, PhysicsComponent physics, EntGotRemovedFromContainerMessage message)
        {
            // If entity being deleted then the parent change will already be handled elsewhere and we don't want to re-add it to the map.
            if (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating) return;

            SetCanCollide(physics, true, false);
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

            _traversal.ProcessMovement();

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
