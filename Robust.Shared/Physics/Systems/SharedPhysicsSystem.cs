using System;
using System.Collections.Generic;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Utility;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Shared.Physics.Systems
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
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedJointSystem _joints = default!;
        [Dependency] private readonly SharedGridTraversalSystem _traversal = default!;
        [Dependency] private readonly SharedDebugPhysicsSystem _debugPhysics = default!;
        [Dependency] private readonly IManifoldManager _manifoldManager = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IIslandManager _islandManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IDependencyCollection _deps = default!;

        public Action<Fixture, Fixture, float, Vector2>? KinematicControllerCollision;

        public bool MetricsEnabled { get; protected set; }

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("physics");
            _sawmill.Level = LogLevel.Info;

            SubscribeLocalEvent<GridAddEvent>(OnGridAdd);
            SubscribeLocalEvent<PhysicsWakeEvent>(OnWake);
            SubscribeLocalEvent<PhysicsSleepEvent>(OnSleep);
            SubscribeLocalEvent<CollisionChangeEvent>(OnCollisionChange);
            SubscribeLocalEvent<PhysicsComponent, EntGotRemovedFromContainerMessage>(HandleContainerRemoved);
            SubscribeLocalEvent<EntParentChangedMessage>(OnParentChange);
            SubscribeLocalEvent<SharedPhysicsMapComponent, ComponentInit>(HandlePhysicsMapInit);
            SubscribeLocalEvent<SharedPhysicsMapComponent, ComponentRemove>(HandlePhysicsMapRemove);
            SubscribeLocalEvent<PhysicsComponent, ComponentInit>(OnPhysicsInit);
            SubscribeLocalEvent<PhysicsComponent, ComponentRemove>(OnPhysicsRemove);
            SubscribeLocalEvent<PhysicsComponent, ComponentGetState>(OnPhysicsGetState);
            SubscribeLocalEvent<PhysicsComponent, ComponentHandleState>(OnPhysicsHandleState);

            _islandManager.Initialize();

            _cfg.OnValueChanged(CVars.AutoClearForces, OnAutoClearChange);
        }

        private void OnPhysicsRemove(EntityUid uid, PhysicsComponent component, ComponentRemove args)
        {
            SetCanCollide(component, false, false);
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
            _deps.InjectDependencies(component);
            component.BroadphaseSystem = _broadphase;
            component.ContactManager = new(_debugPhysics, _manifoldManager, EntityManager, _physicsManager, _cfg);
            component.ContactManager.Initialize();
            component.ContactManager.MapId = component.MapId;
            component.AutoClearForces = _cfg.GetCVar(CVars.AutoClearForces);

            component.ContactManager.KinematicControllerCollision += KinematicControllerCollision;
        }

        private void OnAutoClearChange(bool value)
        {
            var enumerator = AllEntityQuery<SharedPhysicsMapComponent>();

            while (enumerator.MoveNext(out var comp))
            {
                comp.AutoClearForces = value;
            }
        }

        private void HandlePhysicsMapRemove(EntityUid uid, SharedPhysicsMapComponent component, ComponentRemove args)
        {
            // THis entity might be getting deleted before ever having been initialized.
            if (component.ContactManager == null)
                return;

            component.ContactManager.KinematicControllerCollision -= KinematicControllerCollision;
            component.ContactManager.Shutdown();
        }

        private void OnParentChange(ref EntParentChangedMessage args)
        {
            // We do not have a directed/body subscription, because the entity changing parents may not have a physics component, but one of its children might.
            var uid = args.Entity;
            var xform = args.Transform;

            // If this entity has yet to be initialized, then we can skip this as equivalent code will get run during
            // init anyways. HOWEVER: it is possible that one of the children of this entity are already post-init, in
            // which case they still need to handle map changes. This frequently happens when clients receives a server
            // state where a known/old entity gets attached to a new, previously unknown, entity. The new entity will be
            // uninitialized but have an initialized child.
            if (xform.ChildCount == 0 && LifeStage(uid) < EntityLifeStage.Initialized)
                return;

            // Is this entity getting recursively detached after it's parent was already detached to null?
            if (args.OldMapId == MapId.Nullspace && xform.MapID == MapId.Nullspace)
                return;

            var body = CompOrNull<PhysicsComponent>(uid);

            // Handle map changes
            if (args.OldMapId != xform.MapID)
            {
                // This will also handle broadphase updating & joint clearing.
                HandleMapChange(xform, body, args.OldMapId, xform.MapID);
            }

            if (args.OldMapId != xform.MapID)
                return;

            if (body != null)
                HandleParentChangeVelocity(uid, body, ref args, xform);
        }

        /// <summary>
        ///     Recursively add/remove from awake bodies, clear joints, remove from move buffer, and update broadphase.
        /// </summary>
        private void HandleMapChange(TransformComponent xform, PhysicsComponent? body, MapId oldMapId, MapId newMapId)
        {
            var bodyQuery = GetEntityQuery<PhysicsComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var jointQuery = GetEntityQuery<JointComponent>();
            var fixturesQuery = GetEntityQuery<FixturesComponent>();
            var broadQuery = GetEntityQuery<BroadphaseComponent>();

            TryComp(MapManager.GetMapEntityId(oldMapId), out SharedPhysicsMapComponent? oldMap);
            TryComp(MapManager.GetMapEntityId(newMapId), out SharedPhysicsMapComponent? newMap);

            Dictionary<FixtureProxy, Box2>? oldMoveBuffer = null;

            if (oldMap != null)
            {
                oldMoveBuffer = oldMap.MoveBuffer;
            }

            RecursiveMapUpdate(xform, body, newMapId, newMap, oldMap, oldMoveBuffer, bodyQuery, xformQuery, fixturesQuery, jointQuery, broadQuery);
        }

        /// <summary>
        ///     Recursively add/remove from awake bodies, clear joints, remove from move buffer, and update broadphase.
        /// </summary>
        private void RecursiveMapUpdate(
            TransformComponent xform,
            PhysicsComponent? body,
            MapId newMapId,
            SharedPhysicsMapComponent? newMap,
            SharedPhysicsMapComponent? oldMap,
            Dictionary<FixtureProxy, Box2>? oldMoveBuffer,
            EntityQuery<PhysicsComponent> bodyQuery,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<FixturesComponent> fixturesQuery,
            EntityQuery<JointComponent> jointQuery,
            EntityQuery<BroadphaseComponent> broadQuery)
        {
            var uid = xform.Owner;

            DebugTools.Assert(!Deleted(uid));

            // This entity may not have a body, but some of its children might:
            if (body != null)
            {
                if (body.Awake)
                {
                    oldMap?.RemoveSleepBody(body);
                    newMap?.AddAwakeBody(body);
                    DebugTools.Assert(body.Awake);
                }
                else
                    DebugTools.Assert(oldMap?.AwakeBodies.Contains(body) != true);

                // TODO: Could potentially migrate these but would need more thinking
                if (oldMap != null)
                    DestroyContacts(body, oldMap); // This can modify body.Awake
                DebugTools.Assert(body.Contacts.Count == 0);
            }

            if (jointQuery.TryGetComponent(uid, out var joint))
                _joints.ClearJoints(uid, joint);

            var childEnumerator = xform.ChildEnumerator;
            while (childEnumerator.MoveNext(out var child))
            {
                if (xformQuery.TryGetComponent(child, out var childXform))
                {
                    bodyQuery.TryGetComponent(child, out var childBody);
                    RecursiveMapUpdate(childXform, childBody, newMapId, newMap, oldMap, oldMoveBuffer, bodyQuery, xformQuery, fixturesQuery, jointQuery, broadQuery);
                }

            }
        }

        private void OnGridAdd(GridAddEvent ev)
        {
            var guid = ev.EntityUid;

            // If it's mapgrid then no physics.
            if (HasComp<MapComponent>(guid))
                return;

            var body = EnsureComp<PhysicsComponent>(guid);
            SetCanCollide(body, true);
            SetBodyType(body, BodyType.Static);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _cfg.UnsubValueChanged(CVars.AutoClearForces, OnAutoClearChange);
        }

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

            // If this entity is only meant to collide when anchored, return early.
            if (TryComp(uid, out CollideOnAnchorComponent? collideComp) && collideComp.Enable)
                return;

            WakeBody(physics);
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
            var enumerator = AllEntityQuery<SharedPhysicsMapComponent>();

            while (enumerator.MoveNext(out var comp))
            {
                comp.Step(deltaTime, prediction);
            }

            var updateAfterSolve = new PhysicsUpdateAfterSolveEvent(prediction, deltaTime);
            RaiseLocalEvent(ref updateAfterSolve);

            // Enumerator reset
            enumerator = AllEntityQuery<SharedPhysicsMapComponent>();

            // Go through and run all of the deferred events now
            while (enumerator.MoveNext(out var comp))
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
