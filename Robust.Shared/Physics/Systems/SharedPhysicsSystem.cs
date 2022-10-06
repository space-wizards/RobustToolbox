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
        [Dependency] private readonly SharedJointSystem _joints = default!;
        [Dependency] private readonly SharedGridTraversalSystem _traversal = default!;
        [Dependency] private readonly IManifoldManager _collision = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        public Action<Fixture, Fixture, float, Vector2>? KinematicControllerCollision;

        public bool MetricsEnabled { get; protected set; }

        /// <summary>
        /// Used to cache an entity, their local position and local rotation from their <see cref="TransformComponent"/>
        /// Information is cached before the world is simulated to prevent lerping issues with substepping
        /// </summary>
        private Dictionary<EntityUid, (Vector2, Angle)> CachedEntityData = new();

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("physics");
            _sawmill.Level = LogLevel.Info;

            SubscribeLocalEvent<MapChangedEvent>(ev =>
            {
                if (ev.Created)
                    OnMapAdded(ref ev);
            });

            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInit);
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

            _broadphase.UpdateBroadphase(uid, args.OldMapId, xform: xform);

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

            var newBroadphase = _broadphase.GetBroadphase(xform, broadQuery, xformQuery);

            RecursiveMapUpdate(xform, body, newMapId, newBroadphase, newMap, oldMap, oldMoveBuffer, bodyQuery, xformQuery, fixturesQuery, jointQuery, broadQuery);
        }

        /// <summary>
        ///     Recursively add/remove from awake bodies, clear joints, remove from move buffer, and update broadphase.
        /// </summary>
        private void RecursiveMapUpdate(
            TransformComponent xform,
            PhysicsComponent? body,
            MapId newMapId,
            BroadphaseComponent? newBroadphase,
            SharedPhysicsMapComponent? newMap,
            SharedPhysicsMapComponent? oldMap,
            Dictionary<FixtureProxy, Box2>? oldMoveBuffer,
            EntityQuery<PhysicsComponent> bodyQuery,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<FixturesComponent> fixturesQuery,
            EntityQuery<JointComponent> jointQuery,
            EntityQuery<BroadphaseComponent> broadQuery)
        {
            EntityUid? uid = xform.Owner;

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

                // TODO: When we cull sharedphysicsmapcomponent we can probably remove this grid check.
                if (!MapManager.IsGrid(uid.Value) && fixturesQuery.TryGetComponent(uid, out var fixtures) && body._canCollide)
                {
                    // TODO If not deleting, update world position+rotation while iterating through children and pass into UpdateBodyBroadphase
                    _broadphase.UpdateBodyBroadphase(body, fixtures, xform, newBroadphase, xformQuery, oldMoveBuffer);
                }
            }

            if (jointQuery.TryGetComponent(uid, out var joint))
                _joints.ClearJoints(joint);

            if (newMapId != MapId.Nullspace && broadQuery.TryGetComponent(uid, out var parentBroadphase))
                newBroadphase = parentBroadphase;

            var childEnumerator = xform.ChildEnumerator;
            while (childEnumerator.MoveNext(out var child))
            {
                if (xformQuery.TryGetComponent(child, out var childXform))
                {
                    bodyQuery.TryGetComponent(child, out var childBody);
                    RecursiveMapUpdate(childXform, childBody, newMapId, newBroadphase, newMap, oldMap, oldMoveBuffer, bodyQuery, xformQuery, fixturesQuery, jointQuery, broadQuery);
                }

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

        protected abstract void OnMapAdded(ref MapChangedEvent eventArgs);

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
            var targetMinTickrate = (float) _configurationManager.GetCVar(CVars.TargetMinimumTickrate);
            var serverTickrate = (float) _configurationManager.GetCVar(CVars.NetTickrate);
            var substeps = (int)Math.Ceiling(targetMinTickrate / serverTickrate);

            //Grab the transforms and cache the entity, their local position and local rotation to use after the physics step
            foreach (var xformComp in EntityManager.EntityQuery<TransformComponent>(true))
            {
                CachedEntityData.Add(xformComp.Owner, (xformComp.LocalPosition, xformComp.LocalRotation));
            }

            for (int i = 0; i < substeps; i++)
            {
                var frameTime = deltaTime / substeps;

                var updateBeforeSolve = new PhysicsUpdateBeforeSolveEvent(prediction, frameTime);
                RaiseLocalEvent(ref updateBeforeSolve);

                foreach (var comp in EntityManager.EntityQuery<SharedPhysicsMapComponent>(true))
                {
                    comp.Step(frameTime, prediction);

                    _physicsManager.ClearTransforms();
                }

                var updateAfterSolve = new PhysicsUpdateAfterSolveEvent(prediction, frameTime);
                RaiseLocalEvent(ref updateAfterSolve);
            }

            // Go through and run all of the deferred events now
            // Also compares the position pre physics and post physics to fix substep lerping issues
            foreach (var comp in EntityManager.EntityQuery<SharedPhysicsMapComponent>(true))
            {
                comp.ProcessQueue(CachedEntityData);
            }

            _traversal.ProcessMovement();
            CachedEntityData.Clear();
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
