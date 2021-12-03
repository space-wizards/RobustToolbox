using System;
using System.Collections.Generic;
using System.Linq;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Logger = Robust.Shared.Log.Logger;

namespace Robust.Shared.GameObjects
{
    public abstract partial class SharedPhysicsSystem : EntitySystem
    {
        /*
         * TODO:
         * Port acruid's box solver in to reduce allocs for building manifolds (this one is important for perf to remove the disgusting ctors and casts)
         * Raycasts for non-box shapes.
         * SetTransformIgnoreContacts for teleports (and anything else left on the physics body in Farseer)
         * Actual center of mass for shapes (currently just assumes center coordinate)
         * TOI Solver (continuous collision detection)
         * Poly cutting
         * Chain shape
         * (Content) grenade launcher grenades that explode after time rather than impact.
         * pulling prediction
         * When someone yeets out of disposals need to have no collision on that object until they stop colliding
         * A bunch of objects have collision on round start
         * Need a way to specify conditional non-hard collisions (i.e. so items collide with players for IThrowCollide but can still be moved through freely but walls can't collide with them)
         */

        /*
         * Multi-threading notes:
         * Sources:
         * https://github.com/VelcroPhysics/VelcroPhysics/issues/29
         * Aether2D
         * Rapier
         * https://www.slideshare.net/takahiroharada/solver-34909157
         *
         * SO essentially what we should look at doing from what I can discern:
         * Build islands sequentially and then solve them all in parallel (as static bodies are the only thing shared
         * it should be okay given they're never written to)
         * After this, we can then look at doing narrowphase in parallel maybe (at least Aether2D does it) +
         * position constraints in parallel + velocity constraints in parallel
         *
         * The main issue to tackle is graph colouring; Aether2D just seems to use locks for the parallel constraints solver
         * though rapier has a graph colouring implementation (and because of this we should be able to avoid using locks) which we could try using.
         *
         * Given the kind of game SS14 is (our target game I guess) parallelising the islands will probably be the biggest benefit.
         */

        private static readonly Histogram _tickUsageControllerBeforeSolveHistogram = Metrics.CreateHistogram("robust_entity_physics_controller_before_solve",
            "Amount of time spent running a controller's UpdateBeforeSolve", new HistogramConfiguration
            {
                LabelNames = new[] {"controller"},
                Buckets = Histogram.ExponentialBuckets(0.000_001, 1.5, 25)
            });

        private static readonly Histogram _tickUsageControllerAfterSolveHistogram = Metrics.CreateHistogram("robust_entity_physics_controller_after_solve",
            "Amount of time spent running a controller's UpdateAfterSolve", new HistogramConfiguration
            {
                LabelNames = new[] {"controller"},
                Buckets = Histogram.ExponentialBuckets(0.000_001, 1.5, 25)
            });

        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        internal IEnumerable<VirtualController> Controllers => _controllers.Values;
        private readonly Dictionary<Type, VirtualController> _controllers = new();

        public Action<Fixture, Fixture, float, Vector2>? KinematicControllerCollision;

        public bool MetricsEnabled;
        private readonly Stopwatch _stopwatch = new();

        public override void Initialize()
        {
            base.Initialize();
            MapManager.MapCreated += HandleMapCreated;

            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInit);
            SubscribeLocalEvent<PhysicsUpdateMessage>(HandlePhysicsUpdateMessage);
            SubscribeLocalEvent<PhysicsWakeMessage>(HandleWakeMessage);
            SubscribeLocalEvent<PhysicsSleepMessage>(HandleSleepMessage);
            SubscribeLocalEvent<EntMapIdChangedMessage>(HandleMapChange);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerInserted);
            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleContainerRemoved);
            SubscribeLocalEvent<EntParentChangedMessage>(HandleParentChange);
            SubscribeLocalEvent<SharedPhysicsMapComponent, ComponentInit>(HandlePhysicsMapInit);
            SubscribeLocalEvent<SharedPhysicsMapComponent, ComponentRemove>(HandlePhysicsMapRemove);

            BuildControllers();
            Logger.DebugS("physics", $"Found {_controllers.Count} physics controllers.");

            IoCManager.Resolve<IIslandManager>().Initialize();

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.AutoClearForces, OnAutoClearChange, true);
        }

        private void HandlePhysicsMapInit(EntityUid uid, SharedPhysicsMapComponent component, ComponentInit args)
        {
            IoCManager.InjectDependencies(component);
            component.BroadphaseSystem = Get<SharedBroadphaseSystem>();
            component.PhysicsSystem = this;
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

        public T GetController<T>() where T : VirtualController
        {
            return (T) _controllers[typeof(T)];
        }

        private void HandleParentChange(ref EntParentChangedMessage args)
        {
            var entity = args.Entity;

            if (!((!IoCManager.Resolve<IEntityManager>().EntityExists(entity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity.Uid).EntityLifeStage) >= EntityLifeStage.Initialized) ||
                !IoCManager.Resolve<IEntityManager>().TryGetComponent(entity.Uid, out PhysicsComponent? body) ||
                entity.IsInContainer()) return;

            var oldParent = args.OldParent;
            var linearVelocityDiff = Vector2.Zero;
            var angularVelocityDiff = 0f;

            if (oldParent != null && IoCManager.Resolve<IEntityManager>().TryGetComponent(oldParent.Uid, out PhysicsComponent? oldBody))
            {
                var (linear, angular) = oldBody.MapVelocities;

                linearVelocityDiff += linear;
                angularVelocityDiff += angular;
            }

            var newParent = entity.Transform.Parent?.Owner;

            if (newParent != null && IoCManager.Resolve<IEntityManager>().TryGetComponent(newParent.Uid, out PhysicsComponent? newBody))
            {
                var (linear, angular) = newBody.MapVelocities;

                linearVelocityDiff -= linear;
                angularVelocityDiff -= angular;
            }

            body.LinearVelocity += linearVelocityDiff;
            body.AngularVelocity += angularVelocityDiff;
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            if (!EntityManager.TryGetEntity(ev.EntityUid, out var gridEntity)) return;
            var collideComp = gridEntity.EnsureComponent<PhysicsComponent>();
            collideComp.BodyType = BodyType.Static;
        }

        private void BuildControllers()
        {
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();
            var typeFactory = IoCManager.Resolve<IDynamicTypeFactory>();
            var instantiated = new List<VirtualController>();

            foreach (var type in reflectionManager.GetAllChildren(typeof(VirtualController)))
            {
                if (type.IsAbstract)
                    continue;

                instantiated.Add(typeFactory.CreateInstance<VirtualController>(type));
            }

            var nodes = TopologicalSort.FromBeforeAfter(
                instantiated,
                c => c.GetType(),
                c => c,
                c => c.UpdatesBefore,
                c => c.UpdatesAfter);

            var controllers = TopologicalSort.Sort(nodes).ToList();

            foreach (var controller in controllers)
            {
                _controllers[controller.GetType()] = controller;
            }

            foreach (var (_, controller) in _controllers)
            {
                controller.BeforeMonitor = _tickUsageControllerBeforeSolveHistogram.WithLabels(controller.GetType().Name);
                controller.AfterMonitor = _tickUsageControllerAfterSolveHistogram.WithLabels(controller.GetType().Name);
                controller.Initialize();
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            foreach (var (_, controller) in _controllers)
            {
                controller.Shutdown();
            }

            MapManager.MapCreated -= HandleMapCreated;

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.UnsubValueChanged(CVars.AutoClearForces, OnAutoClearChange);
        }

        protected abstract void HandleMapCreated(object? sender, MapEventArgs eventArgs);

        private void HandleMapChange(EntMapIdChangedMessage message)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(message.Entity.Uid, out PhysicsComponent? physicsComponent))
                return;

            Get<SharedJointSystem>().ClearJoints(physicsComponent);
            var oldMapId = message.OldMapId;
            if (oldMapId != MapId.Nullspace)
            {
                IEntity tempQualifier = MapManager.GetMapEntity(oldMapId);
                IoCManager.Resolve<IEntityManager>().GetComponent<SharedPhysicsMapComponent>(tempQualifier.Uid).RemoveBody(physicsComponent);
            }

            var newMapId = message.Entity.Transform.MapID;
            if (newMapId != MapId.Nullspace)
            {
                IEntity tempQualifier = MapManager.GetMapEntity(newMapId);
                IoCManager.Resolve<IEntityManager>().GetComponent<SharedPhysicsMapComponent>(tempQualifier.Uid).AddBody(physicsComponent);
            }
        }

        private void HandlePhysicsUpdateMessage(PhysicsUpdateMessage message)
        {
            var mapId = message.Component.Owner.Transform.MapID;

            if (mapId == MapId.Nullspace)
                return;

            if (message.Component.Deleted || !message.Component.CanCollide)
            {
                IEntity tempQualifier = MapManager.GetMapEntity(mapId);
                IoCManager.Resolve<IEntityManager>().GetComponent<SharedPhysicsMapComponent>(tempQualifier.Uid).RemoveBody(message.Component);
            }
            else
            {
                IEntity tempQualifier = MapManager.GetMapEntity(mapId);
                IoCManager.Resolve<IEntityManager>().GetComponent<SharedPhysicsMapComponent>(tempQualifier.Uid).AddBody(message.Component);
            }
        }

        private void HandleWakeMessage(PhysicsWakeMessage message)
        {
            var mapId = message.Body.Owner.Transform.MapID;

            if (mapId == MapId.Nullspace)
                return;

            IEntity tempQualifier = MapManager.GetMapEntity(mapId);
            IoCManager.Resolve<IEntityManager>().GetComponent<SharedPhysicsMapComponent>(tempQualifier.Uid).AddAwakeBody(message.Body);
        }

        private void HandleSleepMessage(PhysicsSleepMessage message)
        {
            var mapId = message.Body.Owner.Transform.MapID;

            if (mapId == MapId.Nullspace)
                return;

            IEntity tempQualifier = MapManager.GetMapEntity(mapId);
            IoCManager.Resolve<IEntityManager>().GetComponent<SharedPhysicsMapComponent>(tempQualifier.Uid).RemoveSleepBody(message.Body);
        }

        private void HandleContainerInserted(EntInsertedIntoContainerMessage message)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(message.Entity.Uid, out PhysicsComponent? physicsComponent)) return;

            var mapId = message.Container.Owner.Transform.MapID;

            physicsComponent.LinearVelocity = Vector2.Zero;
            physicsComponent.AngularVelocity = 0.0f;
			Get<SharedJointSystem>().ClearJoints(physicsComponent);

            if (mapId != MapId.Nullspace)
            {
                IEntity tempQualifier = MapManager.GetMapEntity(mapId);
                IoCManager.Resolve<IEntityManager>().GetComponent<SharedPhysicsMapComponent>(tempQualifier.Uid).RemoveBody(physicsComponent);
            }
        }

        private void HandleContainerRemoved(EntRemovedFromContainerMessage message)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(message.Entity.Uid, out PhysicsComponent? physicsComponent)) return;

            var mapId = message.Container.Owner.Transform.MapID;

            if (mapId != MapId.Nullspace)
            {
                IEntity tempQualifier = MapManager.GetMapEntity(mapId);
                IoCManager.Resolve<IEntityManager>().GetComponent<SharedPhysicsMapComponent>(tempQualifier.Uid).AddBody(physicsComponent);
            }
        }

        /// <summary>
        ///     Simulates the physical world for a given amount of time.
        /// </summary>
        /// <param name="deltaTime">Delta Time in seconds of how long to simulate the world.</param>
        /// <param name="prediction">Should only predicted entities be considered in this simulation step?</param>
        protected void SimulateWorld(float deltaTime, bool prediction)
        {
            foreach (var (_, controller) in _controllers)
            {
                if (MetricsEnabled)
                {
                    _stopwatch.Restart();
                }
                controller.UpdateBeforeSolve(prediction, deltaTime);
                if (MetricsEnabled)
                {
                    controller.BeforeMonitor.Observe(_stopwatch.Elapsed.TotalSeconds);
                }
            }

            // As controllers may update rotations / positions on their own we can't re-use the cache for finding new contacts
            _broadphaseSystem.EnsureBroadphaseTransforms();

            foreach (var comp in EntityManager.EntityQuery<SharedPhysicsMapComponent>(true))
            {
                comp.Step(deltaTime, prediction);
            }

            foreach (var (_, controller) in _controllers)
            {
                if (MetricsEnabled)
                {
                    _stopwatch.Restart();
                }

                controller.UpdateAfterSolve(prediction, deltaTime);

                if (MetricsEnabled)
                {
                    controller.AfterMonitor.Observe(_stopwatch.Elapsed.TotalSeconds);
                }
            }

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
}
