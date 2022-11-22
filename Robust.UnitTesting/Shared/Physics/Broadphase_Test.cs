using System.Linq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class Broadphase_Test
{
    /// <summary>
    /// If we reparent a sundries entity to another broadphase does it correctly update.
    /// </summary>
    [Test]
    public void ReparentSundries()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();

        var mapId = mapManager.CreateMap();
        var mapEnt = mapManager.GetMapEntityId(mapId);
        var grid = mapManager.CreateGrid(mapId);

        grid.SetTile(Vector2i.Zero, new Tile(1));
        Assert.That(entManager.HasComponent<BroadphaseComponent>(grid.GridEntityId));
        var broadphase = entManager.GetComponent<BroadphaseComponent>(grid.GridEntityId);

        var ent = entManager.SpawnEntity(null, new EntityCoordinates(grid.GridEntityId, new Vector2(0.5f, 0.5f)));
        var xform = entManager.GetComponent<TransformComponent>(ent);
        Assert.That(broadphase.SundriesTree, Does.Contain(ent));

        var broadphaseData = xform.Broadphase;
        Assert.That(broadphaseData!.Value.Uid, Is.EqualTo(grid.GridEntityId));

        xform.Coordinates = new EntityCoordinates(mapEnt, Vector2.One);
        Assert.That(broadphase.SundriesTree, Does.Not.Contain(ent));

        Assert.That(entManager.GetComponent<BroadphaseComponent>(mapEnt).SundriesTree, Does.Contain(ent));
        broadphaseData = xform.Broadphase;
        Assert.That(broadphaseData!.Value.Uid, Is.EqualTo(mapEnt));
    }

    /// <summary>
    /// If we reparent a colliding physics entity to another broadphase does it correctly update.
    /// </summary>
    [Test]
    public void ReparentBroadphase()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();
        var fixturesSystem = entManager.EntitySysManager.GetEntitySystem<FixtureSystem>();
        var physicsSystem = entManager.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>();

        var mapId = mapManager.CreateMap();
        var mapEnt = mapManager.GetMapEntityId(mapId);
        var grid = mapManager.CreateGrid(mapId);

        grid.SetTile(Vector2i.Zero, new Tile(1));
        Assert.That(entManager.HasComponent<BroadphaseComponent>(grid.GridEntityId));
        var broadphase = entManager.GetComponent<BroadphaseComponent>(grid.GridEntityId);

        var ent = entManager.SpawnEntity(null, new EntityCoordinates(grid.GridEntityId, new Vector2(0.5f, 0.5f)));
        var physics = entManager.AddComponent<PhysicsComponent>(ent);
        var xform = entManager.GetComponent<TransformComponent>(ent);

        // If we're not collidable we're still on the sundries tree.
        Assert.That(broadphase.StaticSundriesTree, Does.Contain(ent));
        Assert.That(xform.Broadphase!.Value.Uid, Is.EqualTo(grid.GridEntityId));

        var shape = new PolygonShape();
        shape.SetAsBox(0.5f, 0.5f);
        var fixture = new Fixture(physics, shape);
        fixturesSystem.CreateFixture(physics, fixture);
        physicsSystem.SetCanCollide(physics, true);

        // Now that we're collidable should be correctly on the grid's tree.
        Assert.That(fixture.ProxyCount, Is.EqualTo(1));
        Assert.That(broadphase.StaticSundriesTree, Does.Not.Contain(ent));

        Assert.That(broadphase.StaticTree.GetProxy(fixture.Proxies[0].ProxyId)!.Equals(fixture.Proxies[0]));

        // Now check we go to the map's tree correctly.
        xform.Coordinates = new EntityCoordinates(mapEnt, Vector2.One);
        Assert.That(entManager.GetComponent<BroadphaseComponent>(mapEnt).StaticTree.GetProxy(fixture.Proxies[0].ProxyId)!.Equals(fixture.Proxies[0]));
        Assert.That(xform.Broadphase!.Value.Uid.Equals(mapEnt));
    }

    /// <summary>
    /// If we change a grid's map does it still remain not on the general broadphase.
    /// </summary>
    /// <remarks>
    /// Grids are stored on their own broadphase because moving them is costly.
    /// </remarks>
    [Test]
    public void GridMapUpdate()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();

        var mapId1 = mapManager.CreateMap();
        var mapId2 = mapManager.CreateMap();
        var grid = mapManager.CreateGrid(mapId1);
        var xform = entManager.GetComponent<TransformComponent>(grid.GridEntityId);

        grid.SetTile(Vector2i.Zero, new Tile(1));
        var mapBroadphase1 = entManager.GetComponent<BroadphaseComponent>(mapManager.GetMapEntityId(mapId1));
        var mapBroadphase2 = entManager.GetComponent<BroadphaseComponent>(mapManager.GetMapEntityId(mapId2));
        entManager.TickUpdate(0.016f, false);
#pragma warning disable NUnit2046
        Assert.That(mapBroadphase1.DynamicTree.Count, Is.EqualTo(0));
#pragma warning restore NUnit2046

        xform.Coordinates = new EntityCoordinates(mapManager.GetMapEntityId(mapId2), Vector2.Zero);
        entManager.TickUpdate(0.016f, false);
#pragma warning disable NUnit2046
        Assert.That(mapBroadphase2.DynamicTree.Count, Is.EqualTo(0));
#pragma warning restore NUnit2046
    }

    /// <summary>
    /// If an entity's broadphase is changed are its children's broadphases recursively changed.
    /// </summary>
    [Test]
    public void BroadphaseRecursiveUpdate()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();
        var physicsSystem = sim.Resolve<IEntitySystemManager>().GetEntitySystem<SharedPhysicsSystem>();
        var lookup = sim.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();

        var mapId = mapManager.CreateMap();
        var grid = mapManager.CreateGrid(mapId);

        grid.SetTile(Vector2i.Zero, new Tile(1));
        var gridBroadphase = entManager.GetComponent<BroadphaseComponent>(grid.GridEntityId);
        var mapBroadphase = entManager.GetComponent<BroadphaseComponent>(mapManager.GetMapEntityId(mapId));

        Assert.That(entManager.EntityQuery<BroadphaseComponent>(true).Count(), Is.EqualTo(2));

        var parent = entManager.SpawnEntity(null, new EntityCoordinates(grid.GridEntityId, new Vector2(0.5f, 0.5f)));

        var child1 = entManager.SpawnEntity(null, new EntityCoordinates(parent, Vector2.Zero));
        var child1Xform = entManager.GetComponent<TransformComponent>(child1);

        // Have a non-collidable child and check it doesn't get added too.
        var child2 = entManager.SpawnEntity(null, new EntityCoordinates(child1, Vector2.Zero));
        var child2Xform = entManager.GetComponent<TransformComponent>(child2);
        var child2Body = entManager.AddComponent<PhysicsComponent>(child2);
        physicsSystem.SetCanCollide(child2Body, false);
        Assert.That(!child2Body.CanCollide);

        Assert.That(child1Xform.ParentUid, Is.EqualTo(parent));
        Assert.That(child2Xform.ParentUid, Is.EqualTo(child1));

        Assert.That(lookup.FindBroadphase(parent), Is.EqualTo(gridBroadphase));
        Assert.That(lookup.FindBroadphase(child1), Is.EqualTo(gridBroadphase));

        // They should get deparented to the map and updated to the map's broadphase instead.
        grid.SetTile(Vector2i.Zero, Tile.Empty);
        Assert.That(lookup.FindBroadphase(parent), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child1), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child2Body.Owner), Is.EqualTo(mapBroadphase));
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
        var physSystem = sim.Resolve<IEntitySystemManager>().GetEntitySystem<SharedPhysicsSystem>();
        var lookup = sim.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
        var mapManager = sim.Resolve<IMapManager>();

        var mapId = mapManager.CreateMap();
        var mapUid = mapManager.GetMapEntityId(mapId);
        var mapBroadphase = entManager.GetComponent<BroadphaseComponent>(mapUid);

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
        physSystem.SetCanCollide(child2Body, false);
        Assert.That(!child2Body.CanCollide);

        Assert.That(child1Xform.ParentUid, Is.EqualTo(parent));
        Assert.That(child2Xform.ParentUid, Is.EqualTo(child1));

        Assert.That(lookup.FindBroadphase(parentBody.Owner), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child1Body.Owner), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child2Body.Owner), Is.EqualTo(mapBroadphase));

        // They should get deparented to the map and updated to the map's broadphase instead.
        xformSystem.DetachParentToNull(parentXform);
        Assert.That(lookup.FindBroadphase(parentBody.Owner), Is.EqualTo(null));
        Assert.That(lookup.FindBroadphase(child1Body.Owner), Is.EqualTo(null));
        Assert.That(lookup.FindBroadphase(child2Body.Owner), Is.EqualTo(null));

        // Can't assert CanCollide because they may still want to be valid when coming out of nullspace.

        // Check it goes back to normal
        parentXform.AttachParent(mapUid);
        Assert.That(lookup.FindBroadphase(parentBody.Owner), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child1Body.Owner), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child2Body.Owner), Is.EqualTo(mapBroadphase));
    }
}
