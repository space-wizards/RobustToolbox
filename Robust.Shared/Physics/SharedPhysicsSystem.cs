using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Shared.Physics
{
    public class SharedPhysicsSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public IReadOnlyDictionary<MapId, PhysicsMap> Maps => _maps;

        private List<Type> _controllerTypes = new();

        private Dictionary<MapId, PhysicsMap> _maps = new();

        public override void Initialize()
        {
            base.Initialize();
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();

            foreach (var type in reflectionManager.GetAllChildren(typeof(AetherController)))
            {
                if (type.IsAbstract) continue;
                _controllerTypes.Add(type);
            }
        }

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

        public List<AetherController> GetControllers(PhysicsMap map)
        {
            var typeFactory = IoCManager.Resolve<IDynamicTypeFactory>();
            var result = new List<AetherController>();

            foreach (var type in _controllerTypes)
            {
                var controller = (AetherController) typeFactory.CreateInstance(type);
                controller.World = map;
                result.Add(controller);
            }

            return result;
        }
    }
}
