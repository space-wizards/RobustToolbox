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
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Shared.Physics.Systems
{
    public abstract partial class SharedPhysicsSystem : EntitySystem
    {
        /*
         * TODO:

         * TOI Solver (continuous collision detection)
         * Poly cutting
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

        [Dependency] private readonly IManifoldManager _manifoldManager = default!;
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

        /// <summary>
        /// A variation of <see cref="IGameTiming.CurTime"/> that takes into account the current physics sub-step.
        /// Useful for some entities that need to interpolate their positions during sub-steps.
        /// </summary>
        public TimeSpan? EffectiveCurTime;

        public bool MetricsEnabled { get; protected set; }

        private   EntityQuery<FixturesComponent> _fixturesQuery;
        protected EntityQuery<PhysicsComponent> PhysicsQuery;
        private   EntityQuery<TransformComponent> _xformQuery;
        private   EntityQuery<CollideOnAnchorComponent> _anchorQuery;
        protected EntityQuery<PhysicsMapComponent> PhysMapQuery;
        private   EntityQuery<MapGridComponent> _gridQuery;
        protected EntityQuery<MapComponent> MapQuery;

        private ComponentRegistration _physicsReg = default!;
        private byte _angularVelocityIndex;

        public override void Initialize()
        {
            base.Initialize();

            _physicsReg = EntityManager.ComponentFactory.GetRegistration(CompIdx.Index<PhysicsComponent>());

            // TODO PHYSICS STATE
            // Consider condensing the possible fields into just Linear velocity, angular velocity, and "Other"
            // Or maybe even just "velocity" & "other"
            // Then get-state doesn't have to iterate over a 10-element array.
            // And it simplifies the DirtyField calls.
            // Though I guess combining fixtures & physics will complicate it a bit more again.

            // If you update this then update the delta state + GetState + HandleState!
            EntityManager.ComponentFactory.RegisterNetworkedFields(_physicsReg,
                nameof(PhysicsComponent.CanCollide),
                nameof(PhysicsComponent.BodyStatus),
                nameof(PhysicsComponent.BodyType),
                nameof(PhysicsComponent.SleepingAllowed),
                nameof(PhysicsComponent.FixedRotation),
                nameof(PhysicsComponent.Friction),
                nameof(PhysicsComponent.Force),
                nameof(PhysicsComponent.Torque),
                nameof(PhysicsComponent.LinearDamping),
                nameof(PhysicsComponent.AngularDamping),
                nameof(PhysicsComponent.AngularVelocity),
                nameof(PhysicsComponent.LinearVelocity));

            _angularVelocityIndex = 10;

            _fixturesQuery = GetEntityQuery<FixturesComponent>();
            PhysicsQuery = GetEntityQuery<PhysicsComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();
            _anchorQuery = GetEntityQuery<CollideOnAnchorComponent>();
            PhysMapQuery = GetEntityQuery<PhysicsMapComponent>();
            _gridQuery = GetEntityQuery<MapGridComponent>();
            MapQuery = GetEntityQuery<MapComponent>();

            SubscribeLocalEvent<GridAddEvent>(OnGridAdd);
            SubscribeLocalEvent<CollisionChangeEvent>(OnCollisionChange);
            SubscribeLocalEvent<PhysicsComponent, EntGotRemovedFromContainerMessage>(HandleContainerRemoved);
            SubscribeLocalEvent<PhysicsMapComponent, ComponentInit>(HandlePhysicsMapInit);
            SubscribeLocalEvent<PhysicsComponent, ComponentInit>(OnPhysicsInit);
            SubscribeLocalEvent<PhysicsComponent, ComponentShutdown>(OnPhysicsShutdown);
            SubscribeLocalEvent<PhysicsComponent, ComponentGetState>(OnPhysicsGetState);
            SubscribeLocalEvent<PhysicsComponent, ComponentHandleState>(OnPhysicsHandleState);
            InitializeIsland();
            InitializeContacts();

            Subs.CVar(_cfg, CVars.AutoClearForces, OnAutoClearChange);
            Subs.CVar(_cfg, CVars.NetTickrate, UpdateSubsteps, true);
            Subs.CVar(_cfg, CVars.TargetMinimumTickrate, UpdateSubsteps, true);
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
            var targetMinTickrate = (float) _cfg.GetCVar(CVars.TargetMinimumTickrate);
            var serverTickrate = (float) _cfg.GetCVar(CVars.NetTickrate);
            _substeps = (int)Math.Ceiling(targetMinTickrate / serverTickrate);
        }

        internal void OnParentChange(Entity<TransformComponent, MetaDataComponent> ent, EntityUid oldParent, EntityUid? oldMap)
        {
            // We do not have a directed/body subscription, because the entity changing parents may not have a physics component, but one of its children might.
            var (uid, xform, meta) = ent;

            // If this entity has yet to be initialized, then we can skip this as equivalent code will get run during
            // init anyways. HOWEVER: it is possible that one of the children of this entity are already post-init, in
            // which case they still need to handle map changes. This frequently happens when clients receives a server
            // state where a known/old entity gets attached to a new, previously unknown, entity. The new entity will be
            // uninitialized but have an initialized child.
            if (xform.ChildCount == 0 && meta.EntityLifeStage < EntityLifeStage.Initialized)
                return;

            // Is this entity getting recursively detached after it's parent was already detached to null?
            if (oldMap == null && xform.MapUid == null)
                return;

            var body = PhysicsQuery.CompOrNull(uid);

            // Handle map changes
            if (oldMap != xform.MapUid)
            {
                // This will also handle broadphase updating & joint clearing.
                HandleMapChange(uid, xform, body, oldMap, xform.MapUid);
                return;
            }

            if (body != null)
                HandleParentChangeVelocity(uid, body, oldParent, xform);
        }

        /// <summary>
        ///     Recursively add/remove from awake bodies, clear joints, remove from move buffer, and update broadphase.
        /// </summary>
        private void HandleMapChange(EntityUid uid, TransformComponent xform, PhysicsComponent? body, EntityUid? oldMapId, EntityUid? newMapId)
        {
            PhysMapQuery.TryGetComponent(oldMapId, out var oldMap);
            PhysMapQuery.TryGetComponent(newMapId, out var newMap);
            RecursiveMapUpdate(uid, xform, body, newMap, oldMap);
        }

        /// <summary>
        ///     Recursively add/remove from awake bodies, clear joints, remove from move buffer, and update broadphase.
        /// </summary>
        private void RecursiveMapUpdate(
            EntityUid uid,
            TransformComponent xform,
            PhysicsComponent? body,
            PhysicsMapComponent? newMap,
            PhysicsMapComponent? oldMap)
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

            _joints.ClearJoints(uid);

            foreach (var child in xform._children)
            {
                if (_xformQuery.TryGetComponent(child, out var childXform))
                {
                    PhysicsQuery.TryGetComponent(child, out var childBody);
                    RecursiveMapUpdate(child, childXform, childBody, newMap, oldMap);
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

            EffectiveCurTime = _gameTiming.CurTime;
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

                // TODO PHYSICS Fix Collision Mispredicts
                // If a physics update induces a position update that brings fixtures into contact, the collision starts in the NEXT tick,
                // as positions are updated after CollideContacts() gets called.
                //
                // If a player input induces a position update that brings fixtures into contact, the collision happens on the SAME tick,
                // as inputs are handled before system updates.
                //
                // When applying a server's game state with new positions, the client won't know what caused the positions to update,
                // and thus can't know whether the collision already occurred (i.e., whether its effects are already contained within the
                // game state currently being applied), or whether it should start on the next tick and needs to predict the start of
                // the collision.
                //
                // Currently the client assumes that any position updates happened due to physics steps. I.e., positions are reset, then
                // contacts are reset via ResetContacts(), then positions are updated using the new game state. Alternatively, we could
                // call ResetContacts() AFTER applying the server state, which would correspond to assuming that the collisions have
                // already "started" in the state, and we don't want to re-raise the events.
                //
                // Currently there is no way to avoid mispredicts from happening. E.g., a simple collision-counter component will always
                // either mispredict on physics induced position changes, or on player/input induced updates. The easiest way I can think
                // of to fix this would be to always call `CollideContacts` again at the very end of a physics update.
                // But that might be unnecessarily expensive for what are hopefully only infrequent mispredicts.

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

                EffectiveCurTime = EffectiveCurTime.Value + TimeSpan.FromSeconds(frameTime);
            }

            EffectiveCurTime = null;
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
