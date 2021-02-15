using System;
using System.Collections.Generic;
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
        [Dependency] private readonly IMapManager _mapManager = default!;

        public IReadOnlyDictionary<MapId, PhysicsMap> Maps => _maps;
        private Dictionary<MapId, PhysicsMap> _maps = new();

        internal readonly List<Type> ControllerTypes = new();

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
            BuildControllerGraph();
            Logger.DebugS("physics", $"Found {ControllerTypes.Count} physics controllers.");
        }

        private void BuildControllerGraph()
        {
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();
            var allControllerTypes = new List<Type>();

            foreach (var type in reflectionManager.GetAllChildren(typeof(AetherController)))
            {
                if (type.IsAbstract) continue;
                allControllerTypes.Add(type);
            }

            foreach (var type in allControllerTypes)
            {
                ControllerTypes.Add(type);
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
            foreach (var (mapId, map) in _maps)
            {
                if (mapId == MapId.Nullspace) continue;
                map.Step(deltaTime, prediction);
            }
        }
    }
}
