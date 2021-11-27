using System;
using Castle.Core.Internal;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map;

[TestFixture]
internal class MapGridPauseTests
{
    private readonly MapId _mapId = new(42);

    private static ISimulation SimulationFactory()
    {
        var sim = RobustServerSimulation
            .NewSimulation()
            .InitializeInstance();

        return sim;
    }

    /*
     # Super Map Pausing World

     ## Design
     **Add a pause flag to components (Component.Paused)**

     When an entity is "Paused", this is a flag to prevent the entity from showing up in EntityQueries.
     The idea is that the entity won't "react" to simulation events or be processed by systems, effectively freezing time for them.
     This can be useful for a mapping mode where entities are modified only by direct placement and editor/VV manipulation.

     This should only be internal settable by the engine, calling a special MapManager.PauseMap()
     *Any* time the component's MapId gets changed, this has to be synchronized
     Probably check this with the parent/creation functions
     When MapManager.PauseMap() is called, traverse the entire scene tree of a map, calculate & set the paused flag for each comp

     Add a way to poll paused state on ComponentManager, maybe a way to get the MetaComponent struct?
     Should *GetComponent() also return paused status?

     **Add a pause Override flag on entities (MetaDataComponent.PauseOverride)**

     The override flag is required for special entities like the client observer, which need to still be processed on a paused map for movement.
     This would actually only used by a special non-pausable mapping ghost the client should move around with
     This flag would only have to be polled when writing to the Paused flag.


     ## Details
     pausing a map is a concept outside of the simulation, the state of being paused does not have to be serialized
     pausing is on a per-map basis
     GameTime still passes while entities are Paused, this has nothing to do with server pausing.
     so we want to have a collab of admins setting up a pre-init map while another map has the game running
     so even for in-round gag map collab you need per-map pausing
     we can agree pausing on a per-grid or per-entity basis isn't actually a requirement

     ## Implementation
     components can check their entity for paused state (Entity.Paused -> Map.Paused)
     components have a helper property to check their owner's paused state (Component.Paused -> Entity.Paused)
     Systems can check paused state through the components

     put comp.Deleted and comp.Paused as a val tuple in the component manager so you don't have to deref the entity or component object to poll them
     like Dictionary<EntityUid, (Deleted, Paused, Component)> _entTraitDict
     so you don't have to do component.Owner.Paused every time you access a component
     then pausing an entity would set the Paused flag for all of the components
     because changing paused state happens a lot less than getting components in EntityQueries 
     there is 0 reason to deref the component object if it is paused or deleted
     this gives the ability for anything accessing a component (EventBus, EntityQuery) to early exit without the Component deref
     the downside is that it increases the size of the _entTraitDict entries, leading to more memory access
     this can be mitigated by using a modern CPU that understands array enumeration
     and lets face it, whatever the ECS system is doing is going to eclipse the speed savings
     it is critical that the struct stays <= 16 bytes, also check out the new C# 10/.net 6 dict improvements for accessing structs

     the paused/deleted could be turned into bitflags if there are other things to check
     like we could pack the mapid into the value if that was useful
    
     ## MapEditor
     This system is used by MapInit
     by definition "Paused" is a pre-init map (you sure?)

     ## Resources
     https://github.com/space-wizards/RobustToolbox/issues/1444
     https://github.com/space-wizards/RobustToolbox/issues/1445

     https://discord.com/channels/310555209753690112/310555209753690112/839272383319900220
    */

    /// <summary>
    /// All new maps are created paused. Content needs to manually unpause the map when the round stats.
    /// </summary>
    [Test]
    [Ignore("This is going to be annoying. Maybe in the future?")]
    public void NewMap_CreatedPaused()
    {
        var mapId = new MapId(42);

        var sim = SimulationFactory();
        var mapMan = sim.Resolve<IMapManager>();
        var pauseMan = sim.Resolve<IPauseManager>();

        mapMan.CreateMap(mapId);

        Assert.That(pauseMan.IsMapPaused(mapId), Is.True);
    }

    /// <summary>
    ///     When an entity is on a paused map, it does not get returned by an EntityQuery.
    /// </summary>
    [Test]
    public void Paused_NotInQuery()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();
        var pauseMan = sim.Resolve<IPauseManager>();

        mapMan.CreateMap(_mapId);
        pauseMan.SetMapPaused(_mapId, true);

        entMan.SpawnEntity(null, new MapCoordinates(0, 0, _mapId));

        var results = entMan.EntityQuery<TransformComponent>();
        Assert.That(results.IsNullOrEmpty());
    }

    /// <summary>
    ///     When an entity is on a paused map, EventBus Directed Messages towards the entity's
    ///     components are not received by any subscribers.
    /// </summary>
    [Test]
    [Ignore("This currently breaks a ton of init, like GridInitialize event.")]
    public void Paused_NotReceiveDirectedMessages()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();
        var pauseMan = sim.Resolve<IPauseManager>();

        entMan.EventBus.SubscribeLocalEvent<TransformComponent, DummyEventArgs>((uid, component, args) =>
        {
            Assert.Fail("Received message for a paused entity.");
        });

        mapMan.CreateMap(_mapId);
        pauseMan.SetMapPaused(_mapId, true);

        var node = entMan.SpawnEntity(null, new MapCoordinates(0, 0, _mapId));
        entMan.EventBus.RaiseLocalEvent(node.Uid, new DummyEventArgs());
    }

    private class DummyEventArgs : EntityEventArgs { }

    /// <summary>
    ///     A new child entity added to a paused map will be created paused.
    /// </summary>
    [Test]
    public void Paused_AddEntity_IsPaused()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();
        var pauseMan = sim.Resolve<IPauseManager>();

        // arrange
        mapMan.CreateMap(_mapId);
        pauseMan.SetMapPaused(_mapId, true);

        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, _mapId));

        Assert.That(newEnt.Paused, Is.True);
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
        var pauseMan = sim.Resolve<IPauseManager>();

        // arrange
        mapMan.CreateMap(_mapId);
        pauseMan.SetMapPaused(_mapId, false);

        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, _mapId));

        Assert.That(newEnt.Paused, Is.False);
    }

    /// <summary>
    ///     When a new grid is added to a paused map, the grid becomes paused.
    /// </summary>
    [Test]
    public void Paused_AddGrid_GridPaused()
    {
        var mapId = new MapId(42);

        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();
        var pauseMan = sim.Resolve<IPauseManager>();

        // arrange
        mapMan.CreateMap(mapId);
        pauseMan.SetMapPaused(mapId, true);

        // act
        var newGrid = mapMan.CreateGrid(mapId);

        // assert
        Assert.That(pauseMan.IsMapPaused(mapId), Is.True);
        Assert.That(pauseMan.IsGridPaused(newGrid.Index), Is.True);

        var gridEnt = entMan.GetEntity(newGrid.GridEntityId);
        Assert.That(gridEnt.Paused, NUnit.Framework.Is.True);
    }

    /// <summary>
    ///     When a tree of entities are teleported from a paused map
    ///     to an unpaused map, all of the entities in the tree are unpaused.
    /// </summary>
    [Test]
    public void Paused_TeleportBetweenMaps_Unpaused()
    {
        var mapId2 = new MapId(64);

        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();
        var pauseMan = sim.Resolve<IPauseManager>();

        mapMan.CreateMap(_mapId);
        pauseMan.SetMapPaused(_mapId, true);

        mapMan.CreateMap(mapId2);
        pauseMan.SetMapPaused(mapId2, false);
        var targetMapEnt = mapMan.GetMapEntityId(mapId2);
        
        var node1 = entMan.SpawnEntity(null, new MapCoordinates(0, 0, _mapId)).Transform;
        var node2 = entMan.SpawnEntity(null, new EntityCoordinates(node1.OwnerUid, 0, 0)).Transform;

        node1.ParentUid = targetMapEnt;

        Assert.That(node1.Paused, Is.False);
        Assert.That(node2.Paused, Is.False);
    }

    /// <summary>
    ///     When a tree of entities are teleported from an unpaused map
    ///     to a paused map, all of the entitites in the tree are paused.
    /// </summary>
    [Test]
    public void Unpaused_TeleportBetweenMaps_Paused()
    {
        var mapId2 = new MapId(64);

        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();
        var mapMan = sim.Resolve<IMapManager>();
        var pauseMan = sim.Resolve<IPauseManager>();

        mapMan.CreateMap(_mapId);
        pauseMan.SetMapPaused(_mapId, false);

        mapMan.CreateMap(mapId2);
        pauseMan.SetMapPaused(mapId2, true);
        var targetMapEnt = mapMan.GetMapEntityId(mapId2);

        var node1 = entMan.SpawnEntity(null, new MapCoordinates(0, 0, _mapId)).Transform;
        var node2 = entMan.SpawnEntity(null, new EntityCoordinates(node1.OwnerUid, 0, 0)).Transform;

        node1.ParentUid = targetMapEnt;

        Assert.That(node1.Paused, Is.True);
        Assert.That(node2.Paused, Is.True);
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
        var pauseMan = sim.Resolve<IPauseManager>();

        mapMan.CreateMap(_mapId);
        pauseMan.SetMapPaused(_mapId, true);
        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, _mapId));

        pauseMan.SetMapPaused(_mapId, false);

        Assert.That(newEnt.Paused, Is.False);
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
        var pauseMan = sim.Resolve<IPauseManager>();

        mapMan.CreateMap(_mapId);
        pauseMan.SetMapPaused(_mapId, false);
        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, _mapId));

        pauseMan.SetMapPaused(_mapId, true);

        Assert.That(newEnt.Paused, Is.True);
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
        var pauseMan = sim.Resolve<IPauseManager>();

        mapMan.CreateMap(_mapId);
        pauseMan.SetMapPaused(_mapId, false);
        var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, _mapId));
        newEnt.IgnorePaused = true;

        pauseMan.SetMapPaused(_mapId, true);

        Assert.That(newEnt.Paused, Is.False);
    }
}
