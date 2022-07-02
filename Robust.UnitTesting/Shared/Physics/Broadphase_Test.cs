using System.Linq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class Broadphase_Test
{
    /// <summary>
    /// If an entity's broadphase is changed are its children's broadphases recursively changed.
    /// </summary>
    [Test]
    public void BroadphaseRecursiveUpdate()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();

        var mapId = mapManager.CreateMap();
        var grid = mapManager.CreateGrid(mapId);

        grid.SetTile(Vector2i.Zero, new Tile(1));
        var gridBroadphase = entManager.GetComponent<BroadphaseComponent>(grid.GridEntityId);
        var mapBroapdhase = entManager.GetComponent<BroadphaseComponent>(mapManager.GetMapEntityId(mapId));

        Assert.That(entManager.EntityQuery<BroadphaseComponent>(true).Count(), Is.EqualTo(2));

        var parent = entManager.SpawnEntity(null, new EntityCoordinates(grid.GridEntityId, new Vector2(0.5f, 0.5f)));
        var parentBody = entManager.AddComponent<PhysicsComponent>(parent);

        var child1 = entManager.SpawnEntity(null, new EntityCoordinates(parent, Vector2.Zero));
        var child1Xform = entManager.GetComponent<TransformComponent>(child1);
        var child1Body = entManager.AddComponent<PhysicsComponent>(child1);

        // Have a non-collidable child and check it doesn't get added too.
        var child2 = entManager.SpawnEntity(null, new EntityCoordinates(child1, Vector2.Zero));
        var child2Xform = entManager.GetComponent<TransformComponent>(child2);
        var child2Body = entManager.AddComponent<PhysicsComponent>(child2);
        child2Body.CanCollide = false;
        Assert.That(!child2Body.CanCollide);
        Assert.That(child2Body.Broadphase, Is.EqualTo(null));

        Assert.That(child1Xform.ParentUid, Is.EqualTo(parent));
        Assert.That(child2Xform.ParentUid, Is.EqualTo(child1));

        Assert.That(parentBody.Broadphase, Is.EqualTo(gridBroadphase));
        Assert.That(child1Body.Broadphase, Is.EqualTo(gridBroadphase));

        // They should get deparented to the map and updated to the map's broadphase instead.
        grid.SetTile(Vector2i.Zero, Tile.Empty);
        Assert.That(parentBody.Broadphase, Is.EqualTo(mapBroapdhase));
        Assert.That(child1Body.Broadphase, Is.EqualTo(mapBroapdhase));
        Assert.That(child2Body.Broadphase, Is.EqualTo(null));
    }

    /// <summary>
    /// If an entity's broadphase is changed to nullspace are its children updated.
    /// </summary>
    [Test]
    public void BroadphaseRecursiveNullspaceUpdate()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var xformSystem = sim.Resolve<IEntitySystemManager>().GetEntitySystem<SharedTransformSystem>();
        var mapManager = sim.Resolve<IMapManager>();

        var mapId = mapManager.CreateMap();
        var mapUid = mapManager.GetMapEntityId(mapId);
        var mapBroapdhase = entManager.GetComponent<BroadphaseComponent>(mapUid);

        Assert.That(entManager.EntityQuery<BroadphaseComponent>(true).Count(), Is.EqualTo(1));

        var parent = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var parentXform = entManager.GetComponent<TransformComponent>(parent);
        var parentBody = entManager.AddComponent<PhysicsComponent>(parent);

        var child1 = entManager.SpawnEntity(null, new EntityCoordinates(parent, Vector2.Zero));
        var child1Xform = entManager.GetComponent<TransformComponent>(child1);
        var child1Body = entManager.AddComponent<PhysicsComponent>(child1);

        // Have a non-collidable child and check it doesn't get added too.
        var child2 = entManager.SpawnEntity(null, new EntityCoordinates(child1, Vector2.Zero));
        var child2Xform = entManager.GetComponent<TransformComponent>(child2);
        var child2Body = entManager.AddComponent<PhysicsComponent>(child2);
        child2Body.CanCollide = false;
        Assert.That(!child2Body.CanCollide);

        Assert.That(child1Xform.ParentUid, Is.EqualTo(parent));
        Assert.That(child2Xform.ParentUid, Is.EqualTo(child1));

        Assert.That(parentBody.Broadphase, Is.EqualTo(mapBroapdhase));
        Assert.That(child1Body.Broadphase, Is.EqualTo(mapBroapdhase));
        Assert.That(child2Body.Broadphase, Is.EqualTo(null));

        // They should get deparented to the map and updated to the map's broadphase instead.
        xformSystem.DetachParentToNull(parentXform);
        Assert.That(parentBody.Broadphase, Is.EqualTo(null));
        Assert.That(child1Body.Broadphase, Is.EqualTo(null));
        Assert.That(child2Body.Broadphase, Is.EqualTo(null));

        // Can't assert CanCollide because they may still want to be valid when coming out of nullspace.

        // Check it goes back to normal
        parentXform.AttachParent(mapUid);
        Assert.That(parentBody.Broadphase, Is.EqualTo(mapBroapdhase));
        Assert.That(child1Body.Broadphase, Is.EqualTo(mapBroapdhase));
        Assert.That(child2Body.Broadphase, Is.EqualTo(null));
    }
}
