using System.Linq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map;

[TestFixture]
internal sealed class MapPauseTests
{
    private static ISimulation SimulationFactory()
    {
        var sim = RobustServerSimulation
            .NewSimulation()
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
        var mapId = sim.CreateMap().Uid;
        entMan.System<SharedMapSystem>().SetPaused(mapId, true);
        entMan.SpawnEntity(null, new EntityCoordinates(mapId, default));

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
        var mapId = sim.CreateMap().Uid;
        entMan.System<SharedMapSystem>().SetPaused(mapId, false);
        entMan.SpawnEntity(null, new EntityCoordinates(mapId, default));

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
        var mapId = sim.CreateMap().Uid;
        entMan.System<SharedMapSystem>().SetPaused(mapId, true);
        entMan.SpawnEntity(null, new EntityCoordinates(mapId, default));

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
        var mapId = sim.CreateMap().Uid;
        entMan.System<SharedMapSystem>().SetPaused(mapId, true);
        var newEnt = entMan.SpawnEntity(null, new EntityCoordinates(mapId, default));

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
        var mapId = sim.CreateMap().Uid;
        entMan.System<SharedMapSystem>().SetPaused(mapId, false);
        var newEnt = entMan.SpawnEntity(null, new EntityCoordinates(mapId, default));

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
        var mapId = sim.CreateMap().MapId;
        entMan.System<SharedMapSystem>().SetPaused(mapId, true);

        // act
        var newGrid = mapMan.CreateGridEntity(mapId);

        // assert
        var metaData = entMan.GetComponent<MetaDataComponent>(newGrid);
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
        var map1 = sim.CreateMap().Uid;
        entMan.System<SharedMapSystem>().SetPaused(map1, true);
        var newEnt = entMan.SpawnEntity(null, new EntityCoordinates(map1, default));
        var xform = entMan.GetComponent<TransformComponent>(newEnt);

        var map2 = sim.CreateMap().Uid;
        entMan.System<SharedMapSystem>().SetPaused(map2, false);

        // Act
        entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().SetParent(xform.Owner, map2);

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
        var map1 = sim.CreateMap().Uid;
        entMan.System<SharedMapSystem>().SetPaused(map1, false);
        var newEnt = entMan.SpawnEntity(null, new EntityCoordinates(map1, default));
        var xform = entMan.GetComponent<TransformComponent>(newEnt);

        var map2 = sim.CreateMap().Uid;
        entMan.System<SharedMapSystem>().SetPaused(map2, true);

        // Act
        entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().SetParent(xform.Owner, map2);

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

        var mapId = sim.CreateMap().Uid;
        entMan.System<SharedMapSystem>().SetPaused(mapId, true);
        var newEnt = entMan.SpawnEntity(null, new EntityCoordinates(mapId, default));

        entMan.System<SharedMapSystem>().SetPaused(mapId, false);

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

        var mapId = sim.CreateMap().Uid;
        entMan.System<SharedMapSystem>().SetPaused(mapId, false);
        var newEnt = entMan.SpawnEntity(null, new EntityCoordinates(mapId, default));

        entMan.System<SharedMapSystem>().SetPaused(mapId, true);

        var metaData = entMan.GetComponent<MetaDataComponent>(newEnt);
        Assert.That(metaData.EntityPaused, Is.True);
    }
}
