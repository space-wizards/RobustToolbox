using System;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Threading;
using Robust.Shared.Utility;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Shared.Physics.Systems
{
    public abstract partial class SharedPhysicsSystem : EntitySystem
    {
        /*
         * TODO:

         * Raycasts for non-box shapes.
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

        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IManifoldManager _manifoldManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IParallelManager _parallel = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IDependencyCollection _deps = default!;
        [Dependency] private readonly Gravity2DController _gravity = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedDebugPhysicsSystem _debugPhysics = default!;
        [Dependency] private readonly SharedJointSystem _joints = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly CollisionWakeSystem _wakeSystem = default!;

        private int _substeps;

        public bool MetricsEnabled { get; protected set; }

        private EntityQuery<FixturesComponent> _fixturesQuery;
        protected EntityQuery<PhysicsComponent> PhysicsQuery;
        private EntityQuery<TransformComponent> _xformQuery;
        private EntityQuery<CollideOnAnchorComponent> _anchorQuery;
        protected EntityQuery<PhysicsMapComponent> PhysMapQuery;
        protected EntityQuery<MapComponent> MapQuery;

        public override void Initialize()
        {
            base.Initialize();

            _fixturesQuery = GetEntityQuery<FixturesComponent>();
            PhysicsQuery = GetEntityQuery<PhysicsComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();
            _anchorQuery = GetEntityQuery<CollideOnAnchorComponent>();
            PhysMapQuery = GetEntityQuery<PhysicsMapComponent>();
            MapQuery = GetEntityQuery<MapComponent>();

            SubscribeLocalEvent<GridAddEvent>(OnGridAdd);
            SubscribeLocalEvent<CollisionChangeEvent>(OnCollisionChange);
            SubscribeLocalEvent<PhysicsComponent, EntGotRemovedFromContainerMessage>(HandleContainerRemoved);
            SubscribeLocalEvent<EntParentChangedMessage>(OnParentChange);
            SubscribeLocalEvent<PhysicsMapComponent, ComponentInit>(HandlePhysicsMapInit);
            SubscribeLocalEvent<PhysicsComponent, ComponentInit>(OnPhysicsInit);
            SubscribeLocalEvent<PhysicsComponent, ComponentShutdown>(OnPhysicsShutdown);
            SubscribeLocalEvent<PhysicsComponent, ComponentGetState>(OnPhysicsGetState);
            SubscribeLocalEvent<PhysicsComponent, ComponentHandleState>(OnPhysicsHandleState);
            InitializeIsland();
            InitializeContacts();

            Subs.CVar(_configManager, CVars.AutoClearForces, OnAutoClearChange);
            Subs.CVar(_configManager, CVars.NetTickrate, UpdateSubsteps, true);
            Subs.CVar(_configManager, CVars.TargetMinimumTickrate, UpdateSubsteps, true);
        }

        private void OnPhysicsShutdown(EntityUid uid, PhysicsComponent component, ComponentShutdown args)
        {
            SetCanCollide(uid, false, false, body: component);
            DebugTools.Assert(!component.Awake);

            if (LifeStage(uid) <= EntityLifeStage.MapInitialized)
                RemComp<FixturesComponent>(uid);
        }

        private void OnCollisionChange(ref CollisionChangeEvent ev)
        {
            var uid = ev.BodyUid;
            var mapId = Transform(uid).MapID;

            if (mapId == MapId.Nullspace)
                return;

            if (!ev.CanCollide)
            {
                DestroyContacts(ev.Body);
            }
        }

        private void HandlePhysicsMapInit(EntityUid uid, PhysicsMapComponent component, ComponentInit args)
        {
            _deps.InjectDependencies(component);
            component.AutoClearForces = _cfg.GetCVar(CVars.AutoClearForces);
        }

        private void OnAutoClearChange(bool value)
        {
            var enumerator = AllEntityQuery<PhysicsMapComponent>();

            while (enumerator.MoveNext(out var comp))
            {
                comp.AutoClearForces = value;
            }
        }

        private void UpdateSubsteps(int obj)
        {
            var targetMinTickrate = (float) _configManager.GetCVar(CVars.TargetMinimumTickrate);
            var serverTickrate = (float) _configManager.GetCVar(CVars.NetTickrate);
            _substeps = (int)Math.Ceiling(targetMinTickrate / serverTickrate);
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
                HandleMapChange(uid, xform, body, args.OldMapId, xform.MapID);
            }

            if (args.OldMapId != xform.MapID)
                return;

            if (body != null)
                HandleParentChangeVelocity(uid, body, ref args, xform);
        }

        /// <summary>
        ///     Recursively add/remove from awake bodies, clear joints, remove from move buffer, and update broadphase.
        /// </summary>
        private void HandleMapChange(EntityUid uid, TransformComponent xform, PhysicsComponent? body, MapId oldMapId, MapId newMapId)
        {
            var jointQuery = GetEntityQuery<JointComponent>();

            PhysMapQuery.TryGetComponent(_mapManager.GetMapEntityId(oldMapId), out var oldMap);
            PhysMapQuery.TryGetComponent(_mapManager.GetMapEntityId(newMapId), out var newMap);

            RecursiveMapUpdate(uid, xform, body, newMap, oldMap, jointQuery);
        }

        /// <summary>
        ///     Recursively add/remove from awake bodies, clear joints, remove from move buffer, and update broadphase.
        /// </summary>
        private void RecursiveMapUpdate(
            EntityUid uid,
            TransformComponent xform,
            PhysicsComponent? body,
            PhysicsMapComponent? newMap,
            PhysicsMapComponent? oldMap,
            EntityQuery<JointComponent> jointQuery)
        {
            DebugTools.Assert(!Deleted(uid));

            // This entity may not have a body, but some of its children might:
            if (body != null)
            {
                if (body.Awake)
                {
                    RemoveSleepBody(uid, body, oldMap);
                    AddAwakeBody(uid, body, newMap);
                    DebugTools.Assert(body.Awake);
                }
                else
                    DebugTools.Assert(oldMap?.AwakeBodies.Contains(body) != true);
            }

            if (jointQuery.TryGetComponent(uid, out var joint))
                _joints.ClearJoints(uid, joint);


            foreach (var child in xform._children)
            {
                if (_xformQuery.TryGetComponent(child, out var childXform))
                {
                    PhysicsQuery.TryGetComponent(child, out var childBody);
                    RecursiveMapUpdate(child, childXform, childBody, newMap, oldMap, jointQuery);
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
            var manager = EnsureComp<FixturesComponent>(guid);

            SetCanCollide(guid, true, manager: manager, body: body);
            SetBodyType(guid, BodyType.Static, manager: manager, body: body);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            ShutdownContacts();
        }

        private void UpdateMapAwakeState(EntityUid uid, PhysicsComponent body)
        {
            if (Transform(uid).MapUid is not {} map)
                return;

            if (body.Awake)
                AddAwakeBody(uid, body, map);
            else
                RemoveSleepBody(uid, body, map);
        }

        private void HandleContainerRemoved(EntityUid uid, PhysicsComponent physics, EntGotRemovedFromContainerMessage message)
        {
            // If entity being deleted then the parent change will already be handled elsewhere and we don't want to re-add it to the map.
            if (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating) return;

            // If this entity is only meant to collide when anchored, return early.
            if (_anchorQuery.TryGetComponent(uid, out var collideComp) && collideComp.Enable)
                return;

            WakeBody(uid, body: physics);
        }

        /// <summary>
        ///     Simulates the physical world for a given amount of time.
        /// </summary>
        /// <param name="deltaTime">Delta Time in seconds of how long to simulate the world.</param>
        /// <param name="prediction">Should only predicted entities be considered in this simulation step?</param>
        protected void SimulateWorld(float deltaTime, bool prediction)
        {
            var frameTime = deltaTime / _substeps;

            for (int i = 0; i < _substeps; i++)
            {
                var updateBeforeSolve = new PhysicsUpdateBeforeSolveEvent(prediction, frameTime);
                RaiseLocalEvent(ref updateBeforeSolve);

                var contactEnumerator = AllEntityQuery<PhysicsMapComponent, TransformComponent>();

                // Find new contacts and (TODO: temporary) update any per-map virtual controllers
                while (contactEnumerator.MoveNext(out var comp, out var xform))
                {
                    // Box2D does this at the end of a step and also here when there's a fixture update.
                    // Given external stuff can move bodies we'll just do this here.
                    _broadphase.FindNewContacts(comp, xform.MapID);

                    var updateMapBeforeSolve = new PhysicsUpdateBeforeMapSolveEvent(prediction, comp, frameTime);
                    RaiseLocalEvent(ref updateMapBeforeSolve);
                }

                CollideContacts();
                var enumerator = AllEntityQuery<PhysicsMapComponent>();

                while (enumerator.MoveNext(out var uid, out var comp))
                {
                    Step(uid, comp, frameTime, prediction);
                }

                var updateAfterSolve = new PhysicsUpdateAfterSolveEvent(prediction, frameTime);
                RaiseLocalEvent(ref updateAfterSolve);

                // On last substep (or main step where no substeps occured) we'll update all of the lerp data.
                if (i == _substeps - 1)
                {
                    enumerator = AllEntityQuery<PhysicsMapComponent>();

                    while (enumerator.MoveNext(out var comp))
                    {
                        FinalStep(comp);
                    }
                }
            }
        }

        protected virtual void FinalStep(PhysicsMapComponent component)
        {

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
