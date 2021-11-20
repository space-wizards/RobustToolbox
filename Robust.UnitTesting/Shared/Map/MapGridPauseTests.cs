using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture]
    internal class MapGridPauseTests
    {
        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .InitializeInstance();

            return sim;
        }

        /*
         ## Resources
         https://github.com/space-wizards/RobustToolbox/issues/1444
         https://github.com/space-wizards/RobustToolbox/issues/1445

         https://discord.com/channels/310555209753690112/310555209753690112/839272383319900220

         ## Design
         Add a pause flag to components (Component.Paused)

         When an entity is "Paused", this is a flag to prevent the entity from showing up in EntityQueries.
         The idea is that the entity won't "react" to simulation events or be processed by systems, effectively freezing time for them.
         This can be useful for a mapping mode where entities are modified only by direct placement and editor/VV manipulation.

         This should only be internal settable by the engine, calling a special MapManager.PauseMap()
         *Any* time the component's MapId gets changed, this has to be synchronized
         Probably check this with the parent/creation functions
         When MapManager.PauseMap() is called, traverse the entire scene graph of a map, calculate & set the paused flag for each comp

         Add a pause Override flag on entities (MetaDataComponent.PauseOverride)

         The override flag is required for special entities like the client observer, which need to still be processed on a paused map for movement.
         This would actually only used by a special non-pausable mapping ghost the client should move around with
         This flag would only have to be polled when writing to the Paused flag.

         Add a way to poll paused state on ComponentManager, maybe a way to get the MetaComponent struct?

         ## Details
         pausing is on a per-map basis
         GameTime still passes while entities are Paused, this has nothing to do with server pausing.
         so we want to have a collab of admins setting up a pre-init map while another map has the game running
         so even for in-round gag map collab you need per-map pausing
         we can agree pausing on a per-grid or per-entity basis isn't actually a requirement

         ## Implementation
         components can check their entity for paused state (Entity.Paused -> MetaDataComponent.Paused)
         components have a helper property to check their owner's paused state (Component.Paused -> Entity.Paused)
         Systems can check paused state through the components

         ## MapEditor
         This system is used by MapInit
         by definition "Paused" is a pre-init map (you sure?)

         put comp.Deleted and comp.Paused as a val tuple in the component manager so you don't have to deref the entity or component object to poll them
         like Dictionary<EntityUid, (Deleted, Paused, Component)> _entTraitDict
         so you don't have to do component.Owner.Paused every time you access a component
         then pausing an entity would set the Paused flag for all of the components
         because changing paused state happens a lot less than getting components in EntityQueries 
         there is 0 reason to deref the component object if it is paused or deleted

         the paused/deleted could be turned into bitflags if there are other things to check
         like we could pack the mapid into the value if that was useful
        */

        [Test]
        public void AddGridToPausedMap_GridPaused()
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
            Assert.That(gridEnt.Paused, Is.True);
        }
    }
}
