using System.Collections.Generic;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Shared.Physics
{
    public class SharedPhysicsSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        // World / PhysicsMap was heavily modified to make a lot of stuff not map specific so some of it was dumped here
        private Dictionary<MapId, List<AetherController>>
            _controllers = new Dictionary<MapId, List<AetherController>>();

        public IReadOnlyDictionary<MapId, PhysicsMap> Maps => _maps;

        private Dictionary<MapId, PhysicsMap> _maps = new Dictionary<MapId, PhysicsMap>();

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var predicted = !_gameTiming.InSimulation || _gameTiming.IsFirstTimePredicted;

            foreach (var (_, map) in _maps)
            {
                map.Step(frameTime);

                // See AutoClearForces
                if (!predicted)
                    map.ClearForces();
            }
        }
    }
}
