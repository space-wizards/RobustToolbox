using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Reflection;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Logger = Robust.Shared.Log.Logger;

namespace Robust.Shared.GameObjects
{
    public abstract class SharedPhysicsSystem : EntitySystem
    {
        /*
         * TODO:
         * Port acruid's box solver in to reduce allocs for building manifolds (this one is important for perf to remove the disgusting ctors and casts)
         * Raycasts for non-box shapes.
         * SetTransformIgnoreContacts for teleports (and anything else left on the physics body in Farseer)
         * Actual center of mass for shapes (currently just assumes center coordinate)
         * Circle offsets to entity.
         * TOI Solver (continuous collision detection)
         * Poly cutting
         * Chain shape
         * (Content) grenade launcher grenades that explode after time rather than impact.
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

        [Dependency] private readonly IMapManager _mapManager = default!;

        public IReadOnlyDictionary<MapId, PhysicsMap> Maps => _maps;
        private Dictionary<MapId, PhysicsMap> _maps = new();

        internal IReadOnlyList<AetherController> Controllers => _controllers;
        private List<AetherController> _controllers = new();

        // TODO: Stoer all the controllers here akshully

        public override void Initialize()
        {
            base.Initialize();

            // Having a nullspace map just makes a bunch of code easier, we just don't iterate on it.
            var nullMap = new PhysicsMap(MapId.Nullspace);
            _maps[MapId.Nullspace] = nullMap;
            nullMap.Initialize();

            _mapManager.MapCreated += HandleMapCreated;
            _mapManager.MapDestroyed += HandleMapDestroyed;

            SubscribeLocalEvent<PhysicsUpdateMessage>(HandlePhysicsUpdateMessage);
            SubscribeLocalEvent<PhysicsWakeMessage>(HandleWakeMessage);
            SubscribeLocalEvent<PhysicsSleepMessage>(HandleSleepMessage);
            SubscribeLocalEvent<EntMapIdChangedMessage>(HandleMapChange);

            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerInserted);
            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleContainerRemoved);
            BuildControllers();
            Logger.DebugS("physics", $"Found {_controllers.Count} physics controllers.");
        }

        private void BuildControllers()
        {
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();
            var typeFactory = IoCManager.Resolve<IDynamicTypeFactory>();
            var allControllerTypes = new List<Type>();

            foreach (var type in reflectionManager.GetAllChildren(typeof(AetherController)))
            {
                if (type.IsAbstract) continue;
                allControllerTypes.Add(type);
            }

            var instantiated = new Dictionary<Type, AetherController>();

            foreach (var type in allControllerTypes)
            {
                instantiated.Add(type, (AetherController) typeFactory.CreateInstance(type));
            }

            // Build dependency graph, copied from EntitySystemManager *COUGH

            var nodes = new Dictionary<Type, EntitySystemManager.GraphNode<AetherController>>();

            foreach (var (_, controller) in instantiated)
            {
                var node = new EntitySystemManager.GraphNode<AetherController>(controller);
                nodes[controller.GetType()] = node;
            }

            foreach (var (type, node) in nodes)
            {
                foreach (var before in instantiated[type].UpdatesBefore)
                {
                    nodes[before].DependsOn.Add(node);
                }

                foreach (var after in instantiated[type].UpdatesAfter)
                {
                    node.DependsOn.Add(nodes[after]);
                }
            }

            _controllers = GameObjects.EntitySystemManager.TopologicalSort(nodes.Values).Select(c => c.System).ToList();

            foreach (var controller in _controllers)
            {
                controller.Initialize();
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.MapCreated -= HandleMapCreated;
            _mapManager.MapDestroyed -= HandleMapDestroyed;

            UnsubscribeLocalEvent<PhysicsUpdateMessage>();
            UnsubscribeLocalEvent<PhysicsWakeMessage>();
            UnsubscribeLocalEvent<PhysicsSleepMessage>();
            UnsubscribeLocalEvent<EntMapIdChangedMessage>();

            UnsubscribeLocalEvent<EntInsertedIntoContainerMessage>();
            UnsubscribeLocalEvent<EntRemovedFromContainerMessage>();
        }

        private void HandleMapCreated(object? sender, MapEventArgs eventArgs)
        {
            // Server just creates nullspace map on its own but sends it to client hence we will just ignore it.
            if (_maps.ContainsKey(eventArgs.Map)) return;

            var map = new PhysicsMap(eventArgs.Map);
            _maps.Add(eventArgs.Map, map);
            map.Initialize();
            Logger.DebugS("physics", $"Created physics map for {eventArgs.Map}");
        }

        private void HandleMapDestroyed(object? sender, MapEventArgs eventArgs)
        {
            _maps.Remove(eventArgs.Map);
            Logger.DebugS("physics", $"Destroyed physics map for {eventArgs.Map}");
        }

        private void HandleMapChange(EntMapIdChangedMessage message)
        {
            if (!message.Entity.TryGetComponent(out PhysicsComponent? physicsComponent))
                return;

            var oldMapId = message.OldMapId;
            if (oldMapId != MapId.Nullspace)
            {
                _maps[oldMapId].RemoveBody(physicsComponent);
                physicsComponent.ClearJoints();
            }

            var newMapId = message.Entity.Transform.MapID;
            if (newMapId != MapId.Nullspace)
            {
                _maps[newMapId].AddBody(physicsComponent);
            }
        }

        private void HandlePhysicsUpdateMessage(PhysicsUpdateMessage message)
        {
            var mapId = message.Component.Owner.Transform.MapID;

            if (mapId == MapId.Nullspace)
                return;

            if (message.Component.Deleted || !message.Component.CanCollide)
            {
                _maps[mapId].RemoveBody(message.Component);
            }
            else
            {
                _maps[mapId].AddBody(message.Component);
            }
        }

        private void HandleWakeMessage(PhysicsWakeMessage message)
        {
            var mapId = message.Body.Owner.Transform.MapID;

            if (mapId == MapId.Nullspace)
                return;

            _maps[mapId].AddAwakeBody(message.Body);
        }

        private void HandleSleepMessage(PhysicsSleepMessage message)
        {
            var mapId = message.Body.Owner.Transform.MapID;

            if (mapId == MapId.Nullspace)
                return;

            _maps[mapId].RemoveSleepBody(message.Body);
        }

        private void HandleContainerInserted(EntInsertedIntoContainerMessage message)
        {
            if (!message.Entity.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

            var mapId = message.Container.Owner.Transform.MapID;

            _maps[mapId].RemoveBody(physicsComponent);
        }

        private void HandleContainerRemoved(EntRemovedFromContainerMessage message)
        {
            if (!message.Entity.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

            var mapId = message.Container.Owner.Transform.MapID;

            _maps[mapId].AddBody(physicsComponent);
        }

        /// <summary>
        ///     Simulates the physical world for a given amount of time.
        /// </summary>
        /// <param name="deltaTime">Delta Time in seconds of how long to simulate the world.</param>
        /// <param name="prediction">Should only predicted entities be considered in this simulation step?</param>
        protected void SimulateWorld(float deltaTime, bool prediction)
        {
            foreach (var controller in _controllers)
            {
                controller.UpdateBeforeSolve(prediction, deltaTime);
            }

            foreach (var (mapId, map) in _maps)
            {
                if (mapId == MapId.Nullspace) continue;
                map.Step(deltaTime, prediction);
            }

            foreach (var controller in _controllers)
            {
                controller.UpdateAfterSolve(prediction, deltaTime);
            }

            // Go through and run all of the deferred events now
            foreach (var (mapId, map) in _maps)
            {
                if (mapId == MapId.Nullspace) continue;
                map.ProcessQueue();
            }
        }
    }
}
