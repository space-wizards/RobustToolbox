using System;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Components;
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

        [Dependency] private readonly   IConfigurationManager _configManager = default!;
        [Dependency] private readonly   IManifoldManager _manifoldManager = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] private readonly   IParallelManager _parallel = default!;
        [Dependency] private readonly   IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly   IDependencyCollection _deps = default!;
        [Dependency] private readonly   SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly   EntityLookupSystem _lookup = default!;
        [Dependency] private readonly   SharedJointSystem _joints = default!;
        [Dependency] private readonly   SharedGridTraversalSystem _traversal = default!;
        [Dependency] private readonly   SharedTransformSystem _transform = default!;
        [Dependency] private readonly   SharedDebugPhysicsSystem _debugPhysics = default!;

        private int _substeps;
        private float _invDT;
        private float _invDT0;
        public bool AutoClearForces;

        public bool MetricsEnabled { get; protected set; }

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("physics");
            _sawmill.Level = LogLevel.Info;

            SubscribeLocalEvent<GridAddEvent>(OnGridAdd);
            SubscribeLocalEvent<CollisionChangeEvent>(OnCollisionChange);
            SubscribeLocalEvent<PhysicsComponent, EntGotRemovedFromContainerMessage>(HandleContainerRemoved);
            SubscribeLocalEvent<EntParentChangedMessage>(OnParentChange);
            SubscribeLocalEvent<PhysicsMapComponent, ComponentInit>(HandlePhysicsMapInit);
            SubscribeLocalEvent<PhysicsComponent, ComponentInit>(OnPhysicsInit);
            SubscribeLocalEvent<PhysicsComponent, ComponentRemove>(OnPhysicsRemove);
            SubscribeLocalEvent<PhysicsComponent, ComponentGetState>(OnPhysicsGetState);
            SubscribeLocalEvent<PhysicsComponent, ComponentHandleState>(OnPhysicsHandleState);
            InitializeIsland();
            InitializeContacts();

            _configManager.OnValueChanged(CVars.AutoClearForces, OnAutoClearChange, true);
            _configManager.OnValueChanged(CVars.NetTickrate, UpdateSubsteps, true);
            _configManager.OnValueChanged(CVars.TargetMinimumTickrate, UpdateSubsteps, true);
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

        private void HandlePhysicsMapInit(EntityUid uid, PhysicsMapComponent component, ComponentInit args)
        {
            _deps.InjectDependencies(component);
            component.Physics = this;
        }

        private void OnAutoClearChange(bool value)
        {
            AutoClearForces = value;
        }

        private void UpdateSubsteps(int obj)
        {
            var targetMinTickrate = (float) _configManager.GetCVar(CVars.TargetMinimumTickrate);
            var serverTickrate = (float) _configManager.GetCVar(CVars.NetTickrate);
            _substeps = (int) Math.Max(1, Math.Ceiling(targetMinTickrate / serverTickrate));
            _invDT = targetMinTickrate;
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
                HandleMapChange(xform);
            }

            if (args.OldMapId != xform.MapID)
                return;

            if (body != null)
                HandleParentChangeVelocity(uid, body, ref args, xform);
        }

        /// <summary>
        ///     Recursively add/remove from awake bodies, clear joints, remove from move buffer, and update broadphase.
        /// </summary>
        private void HandleMapChange(TransformComponent xform)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            var jointQuery = GetEntityQuery<JointComponent>();

            RecursiveMapUpdate(xform, xformQuery, jointQuery);
        }

        /// <summary>
        ///     Recursively add/remove from awake bodies, clear joints, remove from move buffer, and update broadphase.
        /// </summary>
        private void RecursiveMapUpdate(
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<JointComponent> jointQuery)
        {
            var uid = xform.Owner;
            DebugTools.Assert(!Deleted(uid));

            if (jointQuery.TryGetComponent(uid, out var joint))
                _joints.ClearJoints(uid, joint);

            var childEnumerator = xform.ChildEnumerator;
            while (childEnumerator.MoveNext(out var child))
            {
                if (xformQuery.TryGetComponent(child, out var childXform))
                {
                    RecursiveMapUpdate(childXform, xformQuery, jointQuery);
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

            ShutdownIsland();
            _configManager.UnsubValueChanged(CVars.AutoClearForces, OnAutoClearChange);
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
            var frameTime = deltaTime / _substeps;

            for (int i = 0; i < _substeps; i++)
            {
                var updateBeforeSolve = new PhysicsUpdateBeforeSolveEvent(prediction, frameTime);
                RaiseLocalEvent(ref updateBeforeSolve);

                // TODO: World contacts instead of per-map
                var contactEnumerator = AllEntityQuery<PhysicsMapComponent, TransformComponent>();

                // Find new contacts and (TODO: temporary) update any per-map virtual controllers
                while (contactEnumerator.MoveNext(out var comp, out var xform))
                {
                    // Box2D does this at the end of a step and also here when there's a fixture update.
                    // Given external stuff can move bodies we'll just do this here.
                    _broadphase.FindNewContacts(comp, xform.MapID);
                }

                CollideContacts();

                UpdateLerpData();

                var dtRatio = _invDT0 * frameTime;

                // Integrate velocities, solve velocity constraints, and do integration.
                Solve(frameTime, dtRatio, prediction);

                // TODO: SolveTOI

                // Box2d recommends clearing (if you are) during fixed updates rather than variable if you are using it
                if (AutoClearForces)
                    ClearForces();

                _invDT0 = _invDT;

                var updateAfterSolve = new PhysicsUpdateAfterSolveEvent(prediction, frameTime);
                RaiseLocalEvent(ref updateAfterSolve);


                // On last substep (or main step where no substeps occured) we'll update all of the lerp data.
                if (i == _substeps - 1)
                {
                    FinalStep();
                }

                _traversal.ProcessMovement();
                _physicsManager.ClearTransforms();
            }
        }

        protected virtual void FinalStep()
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
