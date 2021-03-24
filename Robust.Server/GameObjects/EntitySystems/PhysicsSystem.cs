using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Server.GameObjects
{
    [UsedImplicitly]
    public class PhysicsSystem : SharedPhysicsSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            _mapManager.OnGridCreated += HandleGridCreated;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.OnGridCreated -= HandleGridCreated;
        }

        private void HandleGridCreated(GridId gridId)
        {
            if (!EntityManager.TryGetEntity(_mapManager.GetGrid(gridId).GridEntityId, out var gridEntity)) return;
            var grid = _mapManager.GetGrid(gridId);
            var collideComp = gridEntity.AddComponent<PhysicsComponent>();
            collideComp.CanCollide = true;
            collideComp.AddFixture(new Fixture(collideComp, new PhysShapeGrid(grid)) {CollisionMask = MapGridHelpers.CollisionGroup, CollisionLayer = MapGridHelpers.CollisionGroup});
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, false);
        }
    }
}
