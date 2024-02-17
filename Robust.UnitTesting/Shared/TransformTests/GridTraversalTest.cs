using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.TransformTests;

public sealed class GridTraversalTest : RobustIntegrationTest
{
    [Test]
    public async Task TestSpawnTraversal()
    {
        var server = StartServer();
        await server.WaitIdleAsync();

        var mapMan = server.ResolveDependency<IMapManager>();
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var xforms = sEntMan.System<SharedTransformSystem>();
        var mapSys = sEntMan.System<MapSystem>();

        // Set up entities
        MapId mapId = default!;
        EntityUid map = default;
        EntityUid grid = default;
        Vector2 gridMapPos = default;
        await server.WaitPost(() =>
        {
            mapId = mapMan.CreateMap();
            map = mapMan.GetMapEntityId(mapId);

            var gridComp = mapMan.CreateGridEntity(mapId);
            grid = gridComp.Owner;
            mapSys.SetTile(grid, gridComp, Vector2i.Zero, new Tile(1));
            var gridCentre = new EntityCoordinates(grid, .5f, .5f);
            gridMapPos = gridCentre.ToMap(sEntMan, xforms).Position;
        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
        }

        await server.WaitPost(() =>
        {
            // Spawn an entity using map coordinates will get parented to the grid when spawning on the grid.
            var entity = sEntMan.SpawnEntity(null, new MapCoordinates(gridMapPos, mapId));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).MapUid, Is.EqualTo(map));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).GridUid, Is.EqualTo(grid));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).ParentUid, Is.EqualTo(grid));
            sEntMan.Deleted(entity);

            // Spawning using map entity coords will still parent to the grid when spawning on the grid.
            entity = sEntMan.SpawnEntity(null, new EntityCoordinates(map, gridMapPos));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).MapUid, Is.EqualTo(map));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).GridUid, Is.EqualTo(grid));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).ParentUid, Is.EqualTo(grid));
            sEntMan.Deleted(entity);

            // and using local grid coords also works.
            entity = sEntMan.SpawnEntity(null, new EntityCoordinates(grid, 0.5f, 0.5f));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).MapUid, Is.EqualTo(map));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).GridUid, Is.EqualTo(grid));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).ParentUid, Is.EqualTo(grid));
            sEntMan.Deleted(entity);

            // Spawning an entity far away from the grid will leave it parented to the map.
            entity = sEntMan.SpawnEntity(null, new MapCoordinates(new Vector2(100f, 100f), mapId));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).MapUid, Is.EqualTo(map));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).GridUid, Is.Null);
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).ParentUid, Is.EqualTo(map));
            sEntMan.Deleted(entity);

            entity = sEntMan.SpawnEntity(null, new EntityCoordinates(map, new Vector2(100f, 100f)));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).MapUid, Is.EqualTo(map));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).GridUid, Is.Null);
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).ParentUid, Is.EqualTo(map));
            sEntMan.Deleted(entity);

            entity = sEntMan.SpawnEntity(null, new EntityCoordinates(grid, 100f, 100f));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).MapUid, Is.EqualTo(map));
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).GridUid, Is.Null);
            Assert.That(sEntMan.GetComponent<TransformComponent>(entity).ParentUid, Is.EqualTo(map));
            sEntMan.Deleted(entity);
        });
    }
}

