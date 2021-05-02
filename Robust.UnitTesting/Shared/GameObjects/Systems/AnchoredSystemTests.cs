using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects.Systems
{
    [TestFixture, Parallelizable]
    public class AnchoredSystemTests
    {
        private static readonly MapId TestMapId = new(1);
        private static readonly GridId TestGridId = new(1);

        private class Subscriber : IEntityEventSubscriber { }

        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .InitializeInstance();

            var mapManager = sim.Resolve<IMapManager>();

            // Adds the map with id 1, and spawns entity 1 as the map entity.
            mapManager.CreateMap(TestMapId);

            // Add grid 1, as the default grid to anchor things to.
            mapManager.CreateGrid(TestMapId, TestGridId);

            return sim;
        }

        /// <summary>
        /// When an entity is anchored to a grid tile, it's world position is unchanged.
        /// </summary>
        [Test]
        public void Anchored_WorldPosition_Unchanged()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);

            var subscriber = new Subscriber();
            int calledCount = 0;
            entMan.EventBus.SubscribeEvent<MoveEvent>(EventSource.Local, subscriber, MoveEventHandler);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(Vector2.Zero, TestMapId));

            // Act
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            Assert.That(calledCount, Is.EqualTo(0));
            void MoveEventHandler(MoveEvent ev)
            {
                Assert.Fail("MoveEvent raised when anchoring entity.");
                calledCount++;
            }
        }

        // Anchored entities have their world position locked where it is, their rotation locked and rounded to one of the 4 cardinal directions,
        // and their parent changed to the grid they are anchored to. None of these 3 properties can be changed while anchored. Trying to write to these
        // properties will silently fail.

        // Because of these restrictions, they will never raise move or parent events while anchored.

        // Anchored entities are registered as a tile entity to the grid tile they are above to when the flag is set. They are not moved when
        // anchored. Content is free to place and orient the entity where it wants before anchoring.

        // Entities cannot be anchored to space tiles. If a tile is changed to a space tile, all ents anchored to that tile are unanchored.

        // An anchored entity is defined as an entity with the ITransformComponent.Anchored flag set. PhysicsComponent.Anchored is obsolete,
        // And PhysicsComponent.BodyType is not able to be changed by content. PhysicsComponent.BodyType is synchronized with ITransformComponent.Anchored
        // through anchored messages. SnapGridComponent is obsolete.

        // Trying to anchor an entity to a space tile is a no-op.

        // Adding an anchored entity to a container un-anchors it.

        // Adding and removing a physics component should poll ITransformComponent.Anchored for the correct body type. Ents without a physics component can be anchored.
    }
}
