using System;
using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Logger = Robust.Shared.Log.Logger;

namespace Robust.Shared.GameObjects.Systems
{
    public abstract class SharedPhysicsSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <summary>
        ///     Updates to EntityTree etc. that are deferred until the end of physics.
        /// </summary>
        private readonly HashSet<IPhysicsComponent> _deferredUpdates = new();

        private Dictionary<MapId, PhysicsMap> _maps = new();

        public override void Initialize()
        {
            base.Initialize();

            _mapManager.MapCreated += HandleMapCreated;
            _mapManager.MapDestroyed += HandleMapDestroyed;

            SubscribeLocalEvent<PhysicsUpdateMessage>(HandlePhysicsUpdateMessage);
            SubscribeLocalEvent<EntMapIdChangedMessage>(HandleMapChange);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.MapCreated -= HandleMapCreated;
            _mapManager.MapDestroyed -= HandleMapDestroyed;

            UnsubscribeLocalEvent<PhysicsUpdateMessage>();
            UnsubscribeLocalEvent<EntMapIdChangedMessage>();
        }

        private void HandleMapCreated(object? sender, MapEventArgs eventArgs)
        {
            var map = new PhysicsMap();
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

        /// <summary>
        ///     Simulates the physical world for a given amount of time.
        /// </summary>
        /// <param name="deltaTime">Delta Time in seconds of how long to simulate the world.</param>
        /// <param name="prediction">Should only predicted entities be considered in this simulation step?</param>
        protected void SimulateWorld(float deltaTime, bool prediction)
        {
            foreach (var (_, map) in _maps)
            {
                map.Step(deltaTime, prediction);
            }
        }
    }
}
