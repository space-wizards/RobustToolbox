using JetBrains.Annotations;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Server.GameObjects
{
    [UsedImplicitly]
    public class PhysicsSystem : SharedPhysicsSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            _mapManager.OnGridCreated += HandleGridCreated;
            LoadMetricCVar();
            _configurationManager.OnValueChanged(CVars.MetricsEnabled, _ => LoadMetricCVar());
        }

        private void LoadMetricCVar()
        {
            MetricsEnabled = _configurationManager.GetCVar(CVars.MetricsEnabled);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.OnGridCreated -= HandleGridCreated;
        }

        private void HandleGridCreated(MapId mapId, GridId gridId)
        {
            if (!EntityManager.TryGetEntity(_mapManager.GetGrid(gridId).GridEntityId, out var gridEntity)) return;
            var grid = _mapManager.GetGrid(gridId);
            var collideComp = gridEntity.AddComponent<PhysicsComponent>();
            collideComp.CanCollide = true;
            // TODO: FIX THIS SHIT DON'T LET SLOTH MERGE IT REE
            collideComp.BodyType = BodyType.Dynamic;
            collideComp.BodyStatus = BodyStatus.InAir;
            Get<SharedBroadphaseSystem>().CreateFixture(collideComp, new Fixture(collideComp, new PhysShapeGrid(grid)) {CollisionMask = MapGridHelpers.CollisionGroup, CollisionLayer = MapGridHelpers.CollisionGroup});
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, false);
        }
    }
}
