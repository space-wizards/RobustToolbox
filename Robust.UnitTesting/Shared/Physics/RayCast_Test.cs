using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class RayCast_Test
{
    private static TestCaseData[] _rayCases =
    {
        // Ray goes through
        new(new Vector2(0f, 0.5f), Vector2.UnitY * 2f, new Vector2(0f, 1f - PhysicsConstants.PolygonRadius)),

        // Ray stops inside
        new(new Vector2(0f, 0.5f), Vector2.UnitY, new Vector2(0f, 1f - PhysicsConstants.PolygonRadius)),

        // Ray starts inside
        new(new Vector2(0f, 1.5f), Vector2.UnitY, null),

        // No hit
        new(new Vector2(0f, 0.5f), -Vector2.UnitY, null),
    };

    private static TestCaseData[] _shapeCases =
    {
        // Circle
        // - Initial overlap, no shapecast
        new(new PhysShapeCircle(0.5f, Vector2.Zero), new Transform(Vector2.UnitY / 2f, Angle.Zero), Vector2.UnitY, null),

        // - Cast
        new(new PhysShapeCircle(0.5f, Vector2.Zero), new Transform(Vector2.Zero, Angle.Zero), Vector2.UnitY, new Vector2(0f, 1f - PhysicsConstants.PolygonRadius)),

        // - Miss
        new(new PhysShapeCircle(0.5f, Vector2.Zero), new Transform(Vector2.Zero, Angle.Zero), -Vector2.UnitY, null),

        // Polygon
        // - Initial overlap, no shapecast
        new(new SlimPolygon(Box2.UnitCentered), new Transform(Vector2.UnitY / 2f, Angle.Zero), Vector2.UnitY, null),

        // - Cast
        new(new SlimPolygon(Box2.UnitCentered), new Transform(Vector2.Zero, Angle.Zero), Vector2.UnitY, new Vector2(0.5f, 1f - PhysicsConstants.PolygonRadius)),

        // - Miss
        new(new SlimPolygon(Box2.UnitCentered), new Transform(Vector2.Zero, Angle.Zero), -Vector2.UnitY, null),
    };

    [Test, TestCaseSource(nameof(_rayCases))]
    public void RayCast(Vector2 origin, Vector2 direction, Vector2? point)
    {
        var sim = RobustServerSimulation.NewSimulation().RegisterEntitySystems(f =>
        {
            f.LoadExtraSystemType<RayCastSystem>();
        }).InitializeInstance();
        Setup(sim, out var mapId);
        var raycast = sim.System<RayCastSystem>();

        var hits = raycast.CastRayClosest(mapId,
            origin,
            direction,
            new QueryFilter()
            {
                LayerBits = 1,
            });

        if (point == null)
        {
            Assert.That(!hits.Hit);
        }
        else
        {
            Assert.That(hits.Results.First().Point, Is.EqualTo(point.Value));
        }
    }

    [Test, TestCaseSource(nameof(_shapeCases))]
    public void ShapeCast(IPhysShape shape, Transform origin, Vector2 direction, Vector2? point)
    {
        var sim = RobustServerSimulation.NewSimulation().RegisterEntitySystems(f =>
        {
            f.LoadExtraSystemType<RayCastSystem>();
        }).InitializeInstance();
        Setup(sim, out var mapId);
        var raycast = sim.System<RayCastSystem>();

        var hits = raycast.CastShape(mapId,
            shape,
            origin,
            direction,
            new QueryFilter()
            {
                LayerBits = 1,
            },
            RayCastSystem.RayCastAllCallback);

        if (point == null)
        {
            Assert.That(!hits.Hit);
        }
        else
        {
            Assert.That(hits.Results.First().Point, Is.EqualTo(point.Value));
        }
    }

    private void Setup(ISimulation sim, out MapId mapId)
    {
        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();

        sim.System<SharedMapSystem>().CreateMap(out mapId);

        var grid = sim.Resolve<IMapManager>().CreateGridEntity(mapId);

        for (var i = 0; i < 3; i++)
        {
            mapSystem.SetTile(grid, new Vector2i(i, 0), new Tile(1));
        }

        // Spawn a wall in the middle tile.
        var wall = entManager.SpawnEntity(null, new EntityCoordinates(grid.Owner, new Vector2(1.5f, 0.5f)));

        var physics = entManager.AddComponent<PhysicsComponent>(wall);
        var poly = new PolygonShape();
        poly.SetAsBox(Box2.UnitCentered);
        entManager.System<FixtureSystem>().CreateFixture(wall, "fix1", new Fixture(poly, 1, 1, true));

        entManager.System<SharedPhysicsSystem>().SetCanCollide(wall, true, body: physics);
        Assert.That(physics.CanCollide);

        // Rotate it to be vertical
        entManager.System<SharedTransformSystem>().SetLocalRotation(grid.Owner, Angle.FromDegrees(90));
        entManager.System<SharedTransformSystem>().SetLocalPosition(grid.Owner, Vector2.UnitX / 2f);
    }
}
