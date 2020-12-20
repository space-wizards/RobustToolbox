using System.Collections.Generic;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Shared.GameObjects.Systems
{
    public abstract class SharedPhysicsSystem : EntitySystem
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private readonly Dictionary<MapId, PhysicsMap> _physicsMaps = new Dictionary<MapId, PhysicsMap>();

        public override void Initialize()
        {
            base.Initialize();
            _mapManager.MapCreated += MapCreated;
            _mapManager.MapDestroyed += MapDeleted;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.MapCreated -= MapCreated;
            _mapManager.MapDestroyed -= MapDeleted;
        }

        private void MapCreated(object? sender, MapEventArgs eventArgs)
        {
            _physicsMaps.Add(eventArgs.Map, new PhysicsMap());
        }

        private void MapDeleted(object? sender, MapEventArgs eventArgs)
        {
            _physicsMaps.Remove(eventArgs.Map);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            Simulate(frameTime);
        }

        public void Simulate(float frameTime)
        {
            foreach (var (mapId, map) in _physicsMaps)
            {
                if (mapId == MapId.Nullspace) continue;

                map.Solve(TODO);
            }
        }
    }
}
