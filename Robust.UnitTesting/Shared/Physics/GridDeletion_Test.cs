using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics;

/// <summary>
/// Tests moving and deleting a grid.
/// Mainly useful for grid dynamic tree.
/// </summary>
[TestFixture]
public class GridDeletion_Test : RobustIntegrationTest
{
    [Test]
    public async Task GridDeletionTest()
    {
        var server = StartServer();

        await server.WaitIdleAsync();

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        PhysicsComponent physics = default!;
        IMapGrid grid = default!;
        MapId mapId = default!;

        await server.WaitAssertion(() =>
        {
            mapId = mapManager.CreateMap();
            grid = mapManager.CreateGrid(mapId);

            physics = entManager.GetComponent<PhysicsComponent>(grid.GridEntityId);
            physics.BodyType = BodyType.Dynamic;
            physics.LinearVelocity = new Vector2(50f, 0f);
            Assert.That(physics.LinearVelocity.Length, NUnit.Framework.Is.GreaterThan(0f));
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(physics.LinearVelocity.Length, NUnit.Framework.Is.GreaterThan(0f));
            entManager.DeleteEntity(grid.GridEntityId);

            foreach (var _ in mapManager.FindGridsIntersecting(mapId,
                         new Box2(new Vector2(float.MinValue, float.MinValue),
                             new Vector2(float.MaxValue, float.MaxValue))))
            {
            }
        });
    }
}
