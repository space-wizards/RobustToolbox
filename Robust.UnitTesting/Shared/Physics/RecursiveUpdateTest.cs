using System.Numerics;
using NUnit.Framework;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class RecursiveUpdateTest
{
    /// <summary>
    /// Check that the broadphase updates if a an entity is a child of an entity that is in a container.
    /// </summary>
    [Test]
    public void ContainerRecursiveUpdateTest()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();
        var xforms = entManager.System<SharedTransformSystem>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var containers = entManager.System<ContainerSystem>();

        var mapId = sim.CreateMap().MapId;
        var grid = mapManager.CreateGridEntity(mapId);
        var guid = grid.Owner;
        mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
        Assert.That(entManager.HasComponent<BroadphaseComponent>(guid));

        var broadphase = entManager.GetComponent<BroadphaseComponent>(guid);
        var coords = new EntityCoordinates(guid, new Vector2(0.5f, 0.5f));
        var broadData = new BroadphaseData(guid, EntityUid.Invalid, false, false);

        var container = entManager.SpawnEntity(null, coords);
        var containerXform = entManager.GetComponent<TransformComponent>(container);
        Assert.That(broadphase.SundriesTree, Does.Contain(container));
        Assert.That(containerXform.Broadphase, Is.EqualTo(broadData));

        var contained = entManager.SpawnEntity(null, coords);
        var childA = entManager.SpawnEntity(null, MapCoordinates.Nullspace);
        var childB = entManager.SpawnEntity(null, MapCoordinates.Nullspace);

        var containedXform = entManager.GetComponent<TransformComponent>(contained);
        var childAXform = entManager.GetComponent<TransformComponent>(childA);
        var childBXform = entManager.GetComponent<TransformComponent>(childB);

        Assert.That(broadphase.SundriesTree, Does.Contain(contained));
        Assert.That(containedXform.Broadphase, Is.EqualTo(broadData));

        // Attach child A before inserting.
        xforms.SetCoordinates(childA, childAXform, new EntityCoordinates(contained, Vector2.Zero));
        Assert.That(broadphase.SundriesTree, Does.Contain(childA));
        Assert.That(childAXform.Broadphase, Is.EqualTo(broadData));

        // Insert into container.
        var slot = containers.EnsureContainer<ContainerSlot>(container, "test");
        containers.Insert(contained, slot);

        // Attach child B after having inserted.
        xforms.SetCoordinates(childB, childBXform, new EntityCoordinates(contained, Vector2.Zero));

        Assert.That(broadphase.SundriesTree, Does.Contain(container));
        Assert.That(broadphase.SundriesTree, Does.Not.Contain(contained));
        Assert.That(broadphase.SundriesTree, Does.Not.Contain(childA));
        Assert.That(broadphase.SundriesTree, Does.Not.Contain(childB));

        Assert.That(containerXform.Broadphase, Is.EqualTo(broadData));
        Assert.That(containedXform.Broadphase, Is.EqualTo(null));
        Assert.That(childAXform.Broadphase, Is.EqualTo(null));
        Assert.That(childBXform.Broadphase, Is.EqualTo(null));

        Assert.That(containerXform.ParentUid, Is.EqualTo(guid));
        Assert.That(containedXform.ParentUid, Is.EqualTo(container));
        Assert.That(childAXform.ParentUid, Is.EqualTo(contained));
        Assert.That(childBXform.ParentUid, Is.EqualTo(contained));

        // Check that moving the container does not re-add the contained entities to the broadphase.
        var newCoords = new EntityCoordinates(guid, new Vector2(0.25f, 0.25f));
        xforms.SetCoordinates(container, newCoords);

        Assert.That(broadphase.SundriesTree, Does.Contain(container));
        Assert.That(broadphase.SundriesTree, Does.Not.Contain(contained));
        Assert.That(broadphase.SundriesTree, Does.Not.Contain(childA));
        Assert.That(broadphase.SundriesTree, Does.Not.Contain(childB));

        Assert.That(containerXform.Broadphase, Is.EqualTo(broadData));
        Assert.That(containedXform.Broadphase, Is.EqualTo(null));
        Assert.That(childAXform.Broadphase, Is.EqualTo(null));
        Assert.That(childBXform.Broadphase, Is.EqualTo(null));

        Assert.That(containerXform.ParentUid, Is.EqualTo(guid));
        Assert.That(containedXform.ParentUid, Is.EqualTo(container));
        Assert.That(childAXform.ParentUid, Is.EqualTo(contained));
        Assert.That(childBXform.ParentUid, Is.EqualTo(contained));

        // Remove from container.
        containers.Remove(contained, slot);

        Assert.That(broadphase.SundriesTree, Does.Contain(container));
        Assert.That(broadphase.SundriesTree, Does.Contain(contained));
        Assert.That(broadphase.SundriesTree, Does.Contain(childA));
        Assert.That(broadphase.SundriesTree, Does.Contain(childB));

        Assert.That(containerXform.Broadphase, Is.EqualTo(broadData));
        Assert.That(containedXform.Broadphase, Is.EqualTo(broadData));
        Assert.That(childAXform.Broadphase, Is.EqualTo(broadData));
        Assert.That(childBXform.Broadphase, Is.EqualTo(broadData));

        Assert.That(containerXform.ParentUid, Is.EqualTo(guid));
        Assert.That(containedXform.ParentUid, Is.EqualTo(guid));
        Assert.That(childAXform.ParentUid, Is.EqualTo(contained));
        Assert.That(childBXform.ParentUid, Is.EqualTo(contained));

        // Insert back into container.
        containers.Insert(contained, slot);

        Assert.That(broadphase.SundriesTree, Does.Contain(container));
        Assert.That(broadphase.SundriesTree, Does.Not.Contain(contained));
        Assert.That(broadphase.SundriesTree, Does.Not.Contain(childA));
        Assert.That(broadphase.SundriesTree, Does.Not.Contain(childB));

        Assert.That(containerXform.Broadphase, Is.EqualTo(broadData));
        Assert.That(containedXform.Broadphase, Is.EqualTo(null));
        Assert.That(childAXform.Broadphase, Is.EqualTo(null));
        Assert.That(childBXform.Broadphase, Is.EqualTo(null));

        Assert.That(containerXform.ParentUid, Is.EqualTo(guid));
        Assert.That(containedXform.ParentUid, Is.EqualTo(container));
        Assert.That(childAXform.ParentUid, Is.EqualTo(contained));
        Assert.That(childBXform.ParentUid, Is.EqualTo(contained));

        // re-remove from container, but this time WITHOUT changing parent.
        containers.Remove(contained, slot, reparent: false);

        Assert.That(broadphase.SundriesTree, Does.Contain(container));
        Assert.That(broadphase.SundriesTree, Does.Contain(contained));
        Assert.That(broadphase.SundriesTree, Does.Contain(childA));
        Assert.That(broadphase.SundriesTree, Does.Contain(childB));

        Assert.That(containerXform.Broadphase, Is.EqualTo(broadData));
        Assert.That(containedXform.Broadphase, Is.EqualTo(broadData));
        Assert.That(childAXform.Broadphase, Is.EqualTo(broadData));
        Assert.That(childBXform.Broadphase, Is.EqualTo(broadData));

        Assert.That(containerXform.ParentUid, Is.EqualTo(guid));
        Assert.That(containedXform.ParentUid, Is.EqualTo(container));
        Assert.That(childAXform.ParentUid, Is.EqualTo(contained));
        Assert.That(childBXform.ParentUid, Is.EqualTo(contained));
    }

    /// <summary>
    /// Check that the broadphase positions update when moving an entity's parent.
    /// </summary>
    [Test]
    public void RecursiveMoveTest()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();
        var mapSystem = entManager.EntitySysManager.GetEntitySystem<SharedMapSystem>();
        var transforms = entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        var lookup = entManager.EntitySysManager.GetEntitySystem<EntityLookupSystem>();

        var mapId = sim.CreateMap().MapId;
        var map = mapSystem.GetMapOrInvalid(mapId);
        var mapBroadphase = entManager.GetComponent<BroadphaseComponent>(map);

        var coords = new EntityCoordinates(map, new Vector2(0.5f, 0.5f));
        var mapBroadData = new BroadphaseData(map, EntityUid.Invalid, false, false);

        // Set up parent & child
        var parent = entManager.SpawnEntity(null, coords);
        var child = entManager.SpawnEntity(null, new EntityCoordinates(parent, Vector2.Zero));
        var parentXform = entManager.GetComponent<TransformComponent>(parent);
        var childXform = entManager.GetComponent<TransformComponent>(child);

        // Check correct broadphase
        Assert.That(parentXform.ParentUid, Is.EqualTo(map));
        Assert.That(childXform.ParentUid, Is.EqualTo(parent));
        Assert.That(mapBroadphase.SundriesTree, Does.Contain(parent));
        Assert.That(mapBroadphase.SundriesTree, Does.Contain(child));
        Assert.That(parentXform.Broadphase, Is.EqualTo(mapBroadData));
        Assert.That(childXform.Broadphase, Is.EqualTo(mapBroadData));

        // Check that the entities can be found via a lookup.
        var box = Box2.CenteredAround(coords.Position, Vector2.One);
        var ents = lookup.GetEntitiesIntersecting(mapId, box);
        Assert.That(ents, Does.Contain(parent));
        Assert.That(ents, Does.Contain(child));

        // Move the parent far away.
        var farCoords = new EntityCoordinates(map, new Vector2(100f, 100f));
        transforms.SetCoordinates(parent, farCoords);

        // broadphases have not changed
        Assert.That(parentXform.ParentUid, Is.EqualTo(map));
        Assert.That(childXform.ParentUid, Is.EqualTo(parent));
        Assert.That(mapBroadphase.SundriesTree, Does.Contain(parent));
        Assert.That(mapBroadphase.SundriesTree, Does.Contain(child));
        Assert.That(parentXform.Broadphase, Is.EqualTo(mapBroadData));
        Assert.That(childXform.Broadphase, Is.EqualTo(mapBroadData));

        // old lookup area no longer finds anything
        ents = lookup.GetEntitiesIntersecting(mapId, box);
        Assert.That(ents.Count, Is.EqualTo(0));

        // updated lookup area still finds entities
        var farBox = Box2.CenteredAround(farCoords.Position, Vector2.One);
        ents = lookup.GetEntitiesIntersecting(mapId, farBox);
        Assert.That(ents, Does.Contain(parent));
        Assert.That(ents, Does.Contain(child));

        // Try again, but this time with a parent change.
        var grid = mapManager.CreateGridEntity(mapId);
        var guid = grid.Owner;
        mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
        var gridBroadphase = entManager.GetComponent<BroadphaseComponent>(guid);
        var gridBroadData = new BroadphaseData(guid, EntityUid.Invalid, false, false);

        var gridCoords = new EntityCoordinates(map, new Vector2(-100f, -100f));
        transforms.SetCoordinates(guid, gridCoords);

        // Move parent to the grid
        var gridLocal = new EntityCoordinates(guid, new Vector2(0.5f, 0.5f));
        transforms.SetCoordinates(parent, gridLocal);

        // broadphases have now changed to be on the grids broadphase
        Assert.That(parentXform.ParentUid, Is.EqualTo(guid));
        Assert.That(childXform.ParentUid, Is.EqualTo(parent));
        Assert.That(gridBroadphase.SundriesTree, Does.Contain(parent));
        Assert.That(gridBroadphase.SundriesTree, Does.Contain(child));
        Assert.That(mapBroadphase.SundriesTree, Does.Not.Contain(parent));
        Assert.That(mapBroadphase.SundriesTree, Does.Not.Contain(child));
        Assert.That(parentXform.Broadphase, Is.EqualTo(gridBroadData));
        Assert.That(childXform.Broadphase, Is.EqualTo(gridBroadData));

        // old lookup areas no longer finds anything
        ents = lookup.GetEntitiesIntersecting(mapId, box);
        Assert.That(ents.Count, Is.EqualTo(0));
        ents = lookup.GetEntitiesIntersecting(mapId, farBox);
        Assert.That(ents.Count, Is.EqualTo(0));

        // grid lookup works
        var gridBox = Box2.CenteredAround(gridCoords.Position, Vector2.One);
        ents = lookup.GetEntitiesIntersecting(mapId, gridBox);
        Assert.That(ents, Does.Contain(parent));
        Assert.That(ents, Does.Contain(child));

        // Move grid far away
        var newGridCoords = new EntityCoordinates(map, new Vector2(-100f, 100f));
        transforms.SetCoordinates(guid, newGridCoords);

        // Check lookups again
        ents = lookup.GetEntitiesIntersecting(mapId, box);
        Assert.That(ents.Count, Is.EqualTo(0));
        ents = lookup.GetEntitiesIntersecting(mapId, farBox);
        Assert.That(ents.Count, Is.EqualTo(0));
        ents = lookup.GetEntitiesIntersecting(mapId, gridBox);
        Assert.That(ents.Count, Is.EqualTo(0));

        // grid lookup works
        var newGridBox = Box2.CenteredAround(newGridCoords.Position, Vector2.One);
        ents = lookup.GetEntitiesIntersecting(mapId, newGridBox);
        Assert.That(ents, Does.Contain(parent));
        Assert.That(ents, Does.Contain(child));
    }
}
