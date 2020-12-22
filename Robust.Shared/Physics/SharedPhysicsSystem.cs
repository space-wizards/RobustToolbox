using System.Collections.Generic;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;

namespace Robust.Shared.Physics
{
    public class SharedPhysicsSystem : EntitySystem
    {
        // World / PhysicsMap was heavily modified to make a lot of stuff not map specific so some of it was dumped here
        private Dictionary<MapId, List<AetherController>>
            _controllers = new Dictionary<MapId, List<AetherController>>();

        public IReadOnlyDictionary<MapId, PhysicsMap> Maps => _maps;

        private Dictionary<MapId, PhysicsMap> _maps = new Dictionary<MapId, PhysicsMap>();

        /// <summary>
        /// Fires every time a controller is added to the World.
        /// </summary>
        public ControllerDelegate? ControllerAdded;

        /// <summary>
        /// Fires every time a controlelr is removed form the World.
        /// </summary>
        public ControllerDelegate? ControllerRemoved;

        // TODO: Re-do queries and Raycast in a less janky way

        public override void Initialize()
        {
            base.Initialize();

            // TODO: On MapId changed for entities need to call the relevant PhysicsMap functions for shit.
        }

        public void AddController(PhysicsComponent component, AetherController controller)
        {
            var mapId = component.Owner.Transform.MapID;

            controller.World = _maps[mapId];
            _controllers[mapId].Add(controller);

            ControllerAdded?.Invoke(_maps[mapId], controller);
        }

        public void RemoveController(AetherController controller)
        {
            var mapId = component.Owner.Transform.MapID;

            controller.World = _maps[mapId];
            _controllers[mapId].Add(controller);

            ControllerAdded?.Invoke(_maps[mapId], controller);
        }

        public IEnumerable<AetherController> GetControllers(PhysicsMap world)
        {
            return _controllers[world.MapId];
        }
    }
}
