using System.Diagnostics.CodeAnalysis;
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
        var mapSys = entManager.System<SharedMapSystem>();
        var xformSys = entManager.System<SharedTransformSystem>();

        var (mapEnt, mapId) = sim.CreateMap();
        var grid = mapManager.CreateGridEntity(mapId);

        mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));
        Assert.That(entManager.HasComponent<BroadphaseComponent>(grid));
        var broadphase = entManager.GetComponent<BroadphaseComponent>(grid);

        var ent = entManager.SpawnEntity(null, new EntityCoordinates(grid, new Vector2(0.5f, 0.5f)));
        var xform = entManager.GetComponent<TransformComponent>(ent);
        Assert.That(broadphase.SundriesTree, Does.Contain(ent));

        var broadphaseData = xform.Broadphase;
        Assert.That(broadphaseData!.Value.Uid, Is.EqualTo(grid.Owner));

        xformSys.SetCoordinates(ent, new EntityCoordinates(mapEnt, Vector2.One));
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
        var mapSys = entManager.System<SharedMapSystem>();
        var xformSys = entManager.System<SharedTransformSystem>();

        var (mapEnt, mapId) = sim.CreateMap();
        var grid = mapManager.CreateGridEntity(mapId);
        var gridUid = grid.Owner;

        mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));
        Assert.That(entManager.HasComponent<BroadphaseComponent>(gridUid));
        var broadphase = entManager.GetComponent<BroadphaseComponent>(gridUid);

        var ent = entManager.SpawnEntity(null, new EntityCoordinates(gridUid, new Vector2(0.5f, 0.5f)));
        var physics = entManager.AddComponent<PhysicsComponent>(ent);
        var xform = entManager.GetComponent<TransformComponent>(ent);

        // If we're not collidable we're still on the sundries tree.
        Assert.That(broadphase.StaticSundriesTree, Does.Contain(ent));
        Assert.That(xform.Broadphase!.Value.Uid, Is.EqualTo(gridUid));

        var shape = new PolygonShape();
        shape.SetAsBox(0.5f, 0.5f);
        var fixture = new Fixture(shape, 0, 0, true);
        fixturesSystem.CreateFixture(ent, "fix1", fixture, body: physics, xform: xform);
        physicsSystem.SetCanCollide(ent, true, body: physics);
        Assert.That(physics.CanCollide);

        // Now that we're collidable should be correctly on the grid's tree.
        Assert.That(fixture.ProxyCount, Is.EqualTo(1));
        Assert.That(broadphase.StaticSundriesTree, Does.Not.Contain(ent));

        Assert.That(broadphase.StaticTree.GetProxy(fixture.Proxies[0].ProxyId)!.Equals(fixture.Proxies[0]));

        // Now check we go to the map's tree correctly.
        xformSys.SetCoordinates(ent, new EntityCoordinates(mapEnt, Vector2.One));
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
        var mapSys = entManager.System<SharedMapSystem>();
        var xformSys = entManager.System<SharedTransformSystem>();

        var (map1, mapId1) = sim.CreateMap();
        var (map2, _) = sim.CreateMap();
        var grid = mapManager.CreateGridEntity(mapId1);

        mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));
        var mapBroadphase1 = entManager.GetComponent<BroadphaseComponent>(map1);
        var mapBroadphase2 = entManager.GetComponent<BroadphaseComponent>(map2);
        entManager.TickUpdate(0.016f, false);
#pragma warning disable NUnit2046
        Assert.That(mapBroadphase1.DynamicTree.Count, Is.EqualTo(0));
#pragma warning restore NUnit2046

        xformSys.SetCoordinates(grid, new EntityCoordinates(map1, Vector2.One));
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
        var system = entManager.EntitySysManager;
        var physicsSystem = system.GetEntitySystem<SharedPhysicsSystem>();
        var lookup = system.GetEntitySystem<EntityLookupSystem>();
        var mapSys = entManager.System<SharedMapSystem>();

        var (map, mapId) = sim.CreateMap();
        var grid = mapManager.CreateGridEntity(mapId);

        mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));
        var gridBroadphase = entManager.GetComponent<BroadphaseComponent>(grid);
        var mapBroadphase = entManager.GetComponent<BroadphaseComponent>(map);

        Assert.That(entManager.EntityQuery<BroadphaseComponent>(true).Count(), Is.EqualTo(2));

        var parent = entManager.SpawnEntity(null, new EntityCoordinates(grid, new Vector2(0.5f, 0.5f)));

        var child1 = entManager.SpawnEntity(null, new EntityCoordinates(parent, Vector2.Zero));
        var child1Xform = entManager.GetComponent<TransformComponent>(child1);

        // Have a non-collidable child and check it doesn't get added too.
        var child2 = entManager.SpawnEntity(null, new EntityCoordinates(child1, Vector2.Zero));
        var child2Xform = entManager.GetComponent<TransformComponent>(child2);
        var child2Body = entManager.AddComponent<PhysicsComponent>(child2);
        physicsSystem.SetCanCollide(child2, false, body: child2Body);
        Assert.That(!child2Body.CanCollide);

        Assert.That(child1Xform.ParentUid, Is.EqualTo(parent));
        Assert.That(child2Xform.ParentUid, Is.EqualTo(child1));

        Assert.That(lookup.FindBroadphase(parent), Is.EqualTo(gridBroadphase));
        Assert.That(lookup.FindBroadphase(child1), Is.EqualTo(gridBroadphase));

        // They should get deparented to the map and updated to the map's broadphase instead.
        mapSys.SetTile(grid, Vector2i.Zero, Tile.Empty);
        Assert.That(lookup.FindBroadphase(parent), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child1), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child2), Is.EqualTo(mapBroadphase));
    }

    /// <summary>
    /// Check that broadphases properly recursively update when entities move between maps and grids. The broadphase
    /// updating handles grids separately from other entities, this is intended to be an exhaustive check that the
    /// broadphase always gets updated. E.g., this previously failed when a grid moved from one map to another
    /// </summary>
    [Test]
    public void EntMapChangeRecursiveUpdate()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();
        var system = entManager.EntitySysManager;
        var lookup = system.GetEntitySystem<EntityLookupSystem>();
        var xforms = system.GetEntitySystem<SharedTransformSystem>();
        var physSystem = system.GetEntitySystem<SharedPhysicsSystem>();
        var fixtures = system.GetEntitySystem<FixtureSystem>();
        var mapSys = entManager.System<SharedMapSystem>();

        // setup maps
        var (mapA, mapAId) = sim.CreateMap();
        var (mapB, mapBId) = sim.CreateMap();

        // setup grids
        var gridAComp = mapManager.CreateGridEntity(mapAId);
        var gridBComp = mapManager.CreateGridEntity(mapBId);
        var gridCComp = mapManager.CreateGridEntity(mapAId);
        var gridA = gridAComp.Owner;
        var gridB = gridBComp.Owner;
        var gridC = gridCComp.Owner;
        xforms.SetLocalPosition(gridC, new Vector2(10, 10));
        mapSys.SetTile(gridAComp, Vector2i.Zero, new Tile(1));
        mapSys.SetTile(gridBComp, Vector2i.Zero, new Tile(1));
        mapSys.SetTile(gridCComp, Vector2i.Zero, new Tile(1));

        // set up test entities
        var parent = entManager.SpawnEntity(null, new EntityCoordinates(mapA, new Vector2(200,200)));
        var parentXform = entManager.GetComponent<TransformComponent>(parent);
        var child = entManager.SpawnEntity(null, new EntityCoordinates(parent, Vector2.Zero));
        var childXform = entManager.GetComponent<TransformComponent>(child);
        var childBody = entManager.AddComponent<PhysicsComponent>(child);
        var childFixtures = entManager.GetComponent<FixturesComponent>(child);

        // enable collision for the child
        var shape = new PolygonShape();
        shape.SetAsBox(0.5f, 0.5f);
        fixtures.CreateFixture(child, "fix1", new Fixture(shape, 0, 0, false), body: childBody, xform: childXform);
        physSystem.SetCanCollide(child, true, body: childBody);
        Assert.That(childBody.CanCollide);

        // Initially on mapA
        var AssertMap = (EntityUid map, EntityUid otherMap) =>
        {
            var broadphase = entManager.GetComponent<BroadphaseComponent>(map);
            var physMap = entManager.GetComponent<PhysicsMapComponent>(map);
            Assert.That(parentXform.ParentUid == map);
            Assert.That(parentXform.MapUid == map);
            Assert.That(childXform.MapUid == map);
            Assert.That(lookup.FindBroadphase(parent), Is.EqualTo(broadphase));
            Assert.That(lookup.FindBroadphase(child), Is.EqualTo(broadphase));
            Assert.That(parentXform.Broadphase == new BroadphaseData(map, default, false, false));
            Assert.That(childXform.Broadphase == new BroadphaseData(map, map, true, true));
            Assert.That(physMap.MoveBuffer.ContainsKey(childFixtures.Fixtures.First().Value.Proxies.First()));
            var otherPhysMap = entManager.GetComponent<PhysicsMapComponent>(otherMap);
            Assert.That(otherPhysMap.MoveBuffer.Count == 0);
        };
        AssertMap(mapA, mapB);

        // we are now going to test several broadphase updates where we relocate the parent entity such that it moves:
        // - map to map with a map change
        // - map to grid with a map change
        // - grid to grid with a map change
        // - grid to map with a map change
        // - map to grid without a map change
        // - grid to grid without a map change
        // - grid to map without a map change

        // Move to map B (map to map with a map change)
        xforms.SetCoordinates(parent, new EntityCoordinates(mapB, new Vector2(200, 200)));
        AssertMap(mapB, mapA);

        // Move to gridA on mapA (map to grid with a map change)
        xforms.SetCoordinates(parent, new EntityCoordinates(gridA, default));
        var AssertGrid = (EntityUid grid, EntityUid map, EntityUid otherMap) =>
        {
            var broadphase = entManager.GetComponent<BroadphaseComponent>(grid);
            var physMap = entManager.GetComponent<PhysicsMapComponent>(map);
            var gridXform = entManager.GetComponent<TransformComponent>(grid);
            Assert.That(gridXform.ParentUid == map);
            Assert.That(gridXform.MapUid == map);
            Assert.That(parentXform.ParentUid == grid);
            Assert.That(parentXform.MapUid == map);
            Assert.That(childXform.MapUid == map);
            Assert.That(lookup.FindBroadphase(parent), Is.EqualTo(broadphase));
            Assert.That(lookup.FindBroadphase(child), Is.EqualTo(broadphase));
            Assert.That(parentXform.Broadphase == new BroadphaseData(grid, default, false, false));
            Assert.That(childXform.Broadphase == new BroadphaseData(grid, map, true, true));
            Assert.That(physMap.MoveBuffer.ContainsKey(childFixtures.Fixtures.First().Value.Proxies.First()));
            var otherPhysMap = entManager.GetComponent<PhysicsMapComponent>(otherMap);
            Assert.That(otherPhysMap.MoveBuffer.Count == 0);
        };
        AssertGrid(gridA, mapA, mapB);

        // Move to gridB on mapB (grid to grid with a map change)
        xforms.SetCoordinates(parent, new EntityCoordinates(gridB, default));
        AssertGrid(gridB, mapB, mapA);

        // move to mapA (grid to map with a map change)
        xforms.SetCoordinates(parent, new EntityCoordinates(mapA, new Vector2(200, 200)));
        AssertMap(mapA, mapB);

        // move to gridA on mapA (map to grid without a map change)
        xforms.SetCoordinates(parent, new EntityCoordinates(gridA, default));
        AssertGrid(gridA, mapA, mapB);

        // move to gridC on mapA (grid to grid without a map change)
        xforms.SetCoordinates(parent, new EntityCoordinates(gridC, default));
        AssertGrid(gridC, mapA, mapB);

        // move to gridC on mapA (grid to map without a map change)
        xforms.SetCoordinates(parent, new EntityCoordinates(mapA, new Vector2(200, 200)));
        AssertMap(mapA, mapB);

        // Finally, we check if the broadphase updates if the whole grid moves, instead of just the entity
        // first, move it to a grid:
        xforms.SetCoordinates(parent, new EntityCoordinates(gridC, default));
        AssertGrid(gridC, mapA, mapB);

        // then move the grid to a new map:
        xforms.SetCoordinates(gridC, new EntityCoordinates(mapB, new Vector2(200,200)));
        AssertGrid(gridC, mapB, mapA);
    }

    /// <summary>
    /// If an entity's broadphase is changed to nullspace are its children updated.
    /// </summary>
    [Test]
    public void BroadphaseRecursiveNullspaceUpdate()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var system = entManager.EntitySysManager;
        var xformSystem = system.GetEntitySystem<SharedTransformSystem>();
        var physSystem = system.GetEntitySystem<SharedPhysicsSystem>();
        var lookup = system.GetEntitySystem<EntityLookupSystem>();
        var fixtures = system.GetEntitySystem<FixtureSystem>();
        var (mapUid, mapId) = sim.CreateMap();

        var mapBroadphase = entManager.GetComponent<BroadphaseComponent>(mapUid);

        Assert.That(entManager.EntityQuery<BroadphaseComponent>(true).Count(), Is.EqualTo(1));

        var parent = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var parentXform = entManager.GetComponent<TransformComponent>(parent);
        entManager.AddComponent<PhysicsComponent>(parent);

        var child1 = entManager.SpawnEntity(null, new EntityCoordinates(parent, Vector2.Zero));
        var child1Xform = entManager.GetComponent<TransformComponent>(child1);
        var child1Body = entManager.AddComponent<PhysicsComponent>(child1);

        var shape = new PolygonShape();
        shape.SetAsBox(0.5f, 0.5f);
        fixtures.CreateFixture(child1, "fix1", new Fixture(shape, 0, 0, false), body: child1Body, xform: child1Xform);
        physSystem.SetCanCollide(child1, true, body: child1Body);
        Assert.That(child1Body.CanCollide);

        // Have a non-collidable child and check it doesn't get added too.
        var child2 = entManager.SpawnEntity(null, new EntityCoordinates(child1, Vector2.Zero));
        var child2Xform = entManager.GetComponent<TransformComponent>(child2);
        var child2Body = entManager.AddComponent<PhysicsComponent>(child2);
        physSystem.SetCanCollide(child2, false, body: child2Body);
        Assert.That(!child2Body.CanCollide);

        Assert.That(child1Xform.ParentUid, Is.EqualTo(parent));
        Assert.That(child2Xform.ParentUid, Is.EqualTo(child1));

        Assert.That(lookup.FindBroadphase(parent), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child1), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child2), Is.EqualTo(mapBroadphase));

        // They should get deparented to the map and updated to the map's broadphase instead.
        xformSystem.DetachEntity(parent, parentXform);
        Assert.That(lookup.FindBroadphase(parent), Is.EqualTo(null));
        Assert.That(lookup.FindBroadphase(child1), Is.EqualTo(null));
        Assert.That(lookup.FindBroadphase(child2), Is.EqualTo(null));

        // Can't assert CanCollide because they may still want to be valid when coming out of nullspace.

        // Check it goes back to normal
        xformSystem.SetParent(parent, parentXform, mapUid);
        Assert.That(lookup.FindBroadphase(parent), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child1), Is.EqualTo(mapBroadphase));
        Assert.That(lookup.FindBroadphase(child2), Is.EqualTo(mapBroadphase));
    }
}
