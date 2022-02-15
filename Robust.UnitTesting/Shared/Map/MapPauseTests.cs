using System;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Server;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Map;

[TestFixture]
internal sealed class MapPauseTests
{
    private static ISimulation SimulationFactory()
    {
        var sim = RobustServerSimulation
            .NewSimulation()
            .RegisterComponents(factory => factory.RegisterClass<IgnorePauseComponent>())
            .InitializeInstance();

        return sim;
    }

    /// <summary>
    ///     When an entity is on a paused map, it does not get returned by an EntityQuery.
    /// </summary>
    [Test]
    public void Paused_NotIncluded_NotInQuery()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();

        // arrange
        var mapId = mapMan.CreateMap();
        mapMan.SetMapPaused(mapId, true);

        entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));

        var query = entMan.EntityQuery<TransformComponent>(false).ToList();
        
        // 0 ents, map and the spawned one are not returned
        Assert.That(query.Count, Is.EqualTo(0));
    }

    /// <summary>
    ///     When an entity is on an unpaused map, it is returned by an EntityQuery.
    /// </summary>
    [Test]
    public void UnPaused_NotIncluded_InQuery()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();

        // arrange
        var mapId = mapMan.CreateMap();
        mapMan.SetMapPaused(mapId, false);

        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));

        var query = entMan.EntityQuery<TransformComponent>(false).ToList();

        // 2 ents, map and the spawned one
        Assert.That(query.Count, Is.EqualTo(2));
    }

    /// <summary>
    ///     When an entity is on a paused map, it is get returned by an EntityQuery when included.
    /// </summary>
    [Test]
    public void Paused_Included_InQuery()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();

        // arrange
        var mapId = mapMan.CreateMap();
        mapMan.SetMapPaused(mapId, true);

        entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));

        var query = entMan.EntityQuery<TransformComponent>(true).ToList();
        
        // 2 ents, map and the spawned one are returned because includePaused
        Assert.That(query.Count, Is.EqualTo(2));
    }

    /// <summary>
    ///     A new child entity added to a paused map will be created paused.
    /// </summary>
    [Test]
    public void Paused_AddEntity_IsPaused()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();

        // arrange
        var mapId = mapMan.CreateMap();
        mapMan.SetMapPaused(mapId, true);

        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));

        var metaData = entMan.GetComponent<MetaDataComponent>(newEnt);
        Assert.That(metaData.EntityPaused, Is.True);
    }

    /// <summary>
    ///     A new child entity added to an unpaused map will be created unpaused.
    /// </summary>
    [Test]
    public void UnPaused_AddEntity_IsNotPaused()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();

        // arrange
        var mapId = mapMan.CreateMap();
        mapMan.SetMapPaused(mapId, false);

        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));

        var metaData = entMan.GetComponent<MetaDataComponent>(newEnt);
        Assert.That(metaData.EntityPaused, Is.False);
    }

    /// <summary>
    ///     When a new grid is added to a paused map, the grid becomes paused.
    /// </summary>
    [Test]
    public void Paused_AddGrid_GridPaused()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();

        // arrange
        var mapId = mapMan.CreateMap();
        mapMan.SetMapPaused(mapId, true);

        // act
        var newGrid = mapMan.CreateGrid(mapId);

        // assert
        Assert.That(mapMan.IsMapPaused(mapId), Is.True);
        Assert.That(mapMan.IsGridPaused(newGrid.GridEntityId), Is.True);

        var metaData = entMan.GetComponent<MetaDataComponent>(newGrid.GridEntityId);
        Assert.That(metaData.EntityPaused, Is.True);
    }

    /// <summary>
    ///     When a tree of entities are teleported from a paused map
    ///     to an unpaused map, all of the entities in the tree are unpaused.
    /// </summary>
    [Test]
    public void Paused_TeleportBetweenMaps_Unpaused()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();

        // arrange
        var map1 = mapMan.CreateMap();
        mapMan.SetMapPaused(map1, true);

        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, map1));
        var xform = entMan.GetComponent<TransformComponent>(newEnt);

        var map2 = mapMan.CreateMap();
        mapMan.SetMapPaused(map2, false);

        // Act
        xform.ParentUid = mapMan.GetMapEntityId(map2);

        var metaData = entMan.GetComponent<MetaDataComponent>(newEnt);
        Assert.That(metaData.EntityPaused, Is.False);
    }

    /// <summary>
    ///     When a tree of entities are teleported from an unpaused map
    ///     to a paused map, all of the entitites in the tree are paused.
    /// </summary>
    [Test]
    public void Unpaused_TeleportBetweenMaps_IsPaused()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();

        // arrange
        var map1 = mapMan.CreateMap();
        mapMan.SetMapPaused(map1, false);

        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, map1));
        var xform = entMan.GetComponent<TransformComponent>(newEnt);

        var map2 = mapMan.CreateMap();
        mapMan.SetMapPaused(map2, true);

        // Act
        xform.ParentUid = mapMan.GetMapEntityId(map2);

        var metaData = entMan.GetComponent<MetaDataComponent>(newEnt);
        Assert.That(metaData.EntityPaused, Is.True);
    }

    /// <summary>
    ///     When a paused map is unpaused, all of the entities on the map are unpaused.
    /// </summary>
    [Test]
    public void Paused_UnpauseMap_UnpausedEntities()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();

        var mapId = mapMan.CreateMap();
        mapMan.SetMapPaused(mapId, true);
        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));

        mapMan.SetMapPaused(mapId, false);

        var metaData = entMan.GetComponent<MetaDataComponent>(newEnt);
        Assert.That(metaData.EntityPaused, Is.False);
    }

    /// <summary>
    ///     When an unpaused map is paused, all of the entities on the map are paused.
    /// </summary>
    [Test]
    public void Unpaused_PauseMap_PausedEntities()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();

        var mapId = mapMan.CreateMap();
        mapMan.SetMapPaused(mapId, false);
        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));

        mapMan.SetMapPaused(mapId, true);

        var metaData = entMan.GetComponent<MetaDataComponent>(newEnt);
        Assert.That(metaData.EntityPaused, Is.True);
    }

    /// <summary>
    ///     An entity that has set IgnorePause will not be paused when the map is paused.
    /// </summary>
    [Test]
    public void IgnorePause_PauseMap_NotPaused()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();

        var mapId = mapMan.CreateMap();
        mapMan.SetMapPaused(mapId, false);
        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));

        entMan.AddComponent<IgnorePauseComponent>(newEnt);
        mapMan.SetMapPaused(mapId, true);

        var metaData = entMan.GetComponent<MetaDataComponent>(newEnt);
        Assert.That(metaData.EntityPaused, Is.False);
    }

    /// <summary>
    /// An unallocated MapId is always unpaused.
    /// </summary>
    [Test]
    public void UnallocatedMap_IsUnPaused()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = (IMapManagerInternal)sim.Resolve<IMapManager>();

        // some random unallocated MapId
        var paused = mapMan.IsMapPaused(new MapId(12));

        Assert.That(paused, Is.False);
    }

    /// <summary>
    /// Nullspace is always unpaused, and setting it is a no-op.
    /// </summary>
    [Test]
    public void Nullspace_Pause_IsUnPaused()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = (IMapManagerInternal)sim.Resolve<IMapManager>();

        mapMan.SetMapPaused(MapId.Nullspace, true);

        var paused = mapMan.IsMapPaused(MapId.Nullspace);
        Assert.That(paused, Is.False);
    }

    /// <summary>
    /// An allocated MapId without an allocated entity (Nullspace) is always unpaused.
    /// </summary>
    [Test]
    public void Nullspace_IsUnPaused()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = (IMapManagerInternal)sim.Resolve<IMapManager>();

        var paused = mapMan.IsMapPaused(MapId.Nullspace);

        Assert.That(paused, Is.False);
    }

    /// <summary>
    /// A freed MapId is always unpaused.
    /// </summary>
    [Test]
    public void Unpaused_Freed_IsUnPaused()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = (IMapManagerInternal)sim.Resolve<IMapManager>();

        var paused = mapMan.IsMapPaused(MapId.Nullspace);

        Assert.That(paused, Is.False);
    }
}
