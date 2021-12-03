using System;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects.Systems
{
    [TestFixture, Parallelizable]
    public class AnchoredSystemTests
    {
        private static readonly MapId TestMapId = new(1);
        private static readonly GridId TestGridId = new(1);

        private class Subscriber : IEntityEventSubscriber { }

        private const string Prototypes = @"
- type: entity
  name: anchoredEnt
  id: anchoredEnt
  components:
  - type: Transform
    anchored: true";

        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterPrototypes(f=>
                {
                    f.LoadString(Prototypes);
                })
                .InitializeInstance();

            var mapManager = sim.Resolve<IMapManager>();

            // Adds the map with id 1, and spawns entity 1 as the map entity.
            mapManager.CreateMap(TestMapId);

            // Add grid 1, as the default grid to anchor things to.
            mapManager.CreateGrid(TestMapId, TestGridId);

            return sim;
        }

        // An entity is anchored to the tile it is over on the target grid.
        // An entity is anchored by setting the flag on the transform.
        // An anchored entity is defined as an entity with the TransformComponent.Anchored flag set.
        // The Anchored field is used for serialization of anchored state.

        // TODO: The grid SnapGrid functions are internal, expose the query functions to content.
        // PhysicsComponent.BodyType is not able to be changed by content.

        /// <summary>
        /// When an entity is anchored to a grid tile, it's world position is centered on the tile.
        /// Otherwise you can anchor an entity to a tile without the entity actually being on top of the tile.
        /// This movement will trigger a MoveEvent.
        /// </summary>
        [Test]
        public void OnAnchored_WorldPosition_TileCenter()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var coordinates = new MapCoordinates(new Vector2(7, 7), TestMapId);

            // can only be anchored to a tile
            var grid = mapMan.GetGrid(TestGridId);
            grid.SetTile(grid.TileIndicesFor(coordinates), new Tile(1));

            var subscriber = new Subscriber();
            int calledCount = 0;
            var ent1 = entMan.SpawnEntity(null, coordinates); // this raises MoveEvent, subscribe after
            entMan.EventBus.SubscribeEvent<MoveEvent>(EventSource.Local, subscriber, MoveEventHandler);

            // Act
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).WorldPosition, Is.EqualTo(new Vector2(7.5f, 7.5f))); // centered on tile
            Assert.That(calledCount, Is.EqualTo(1)); // because the ent was moved from snapping, a MoveEvent was raised.
            void MoveEventHandler(ref MoveEvent ev)
            {
                calledCount++;
            }
        }

        /// <summary>
        /// When an entity is anchored to a grid tile, it's parent is set to the grid.
        /// </summary>
        [Test]
        public void OnAnchored_Parent_SetToGrid()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var coordinates = new MapCoordinates(new Vector2(7, 7), TestMapId);

            // can only be anchored to a tile
            var grid = mapMan.GetGrid(TestGridId);
            grid.SetTile(grid.TileIndicesFor(coordinates), new Tile(1));

            var subscriber = new Subscriber();
            int calledCount = 0;
            var ent1 = entMan.SpawnEntity(null, coordinates); // this raises MoveEvent, subscribe after
            entMan.EventBus.SubscribeEvent<EntParentChangedMessage>(EventSource.Local, subscriber, ParentChangedHandler);

            // Act
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).ParentUid, Is.EqualTo(grid.GridEntityId));
            Assert.That(calledCount, Is.EqualTo(1));
            void ParentChangedHandler(ref EntParentChangedMessage ev)
            {
                Assert.That(ev.Entity, Is.EqualTo(ent1));
                calledCount++;
            }
        }

        /// <summary>
        /// Entities cannot be anchored to empty tiles. Attempting this is a no-op, and silently fails.
        /// </summary>
        [Test]
        public void OnAnchored_EmptyTile_Nop()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Coordinates);
            grid.SetTile(tileIndices, Tile.Empty);

            // Act
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            Assert.That(grid.GetAnchoredEntities(tileIndices).Count(), Is.EqualTo(0));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.EqualTo(Tile.Empty));
        }

        /// <summary>
        /// Entities can be anchored to any non-empty grid tile. A physics component is not required on either
        /// the grid or the entity to anchor it.
        /// </summary>
        [Test]
        public void OnAnchored_NonEmptyTile_Anchors()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Coordinates);
            grid.SetTile(tileIndices, new Tile(1));

            // Act
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            Assert.That(grid.GetAnchoredEntities(tileIndices).First(), Is.EqualTo(ent1));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.Not.EqualTo(Tile.Empty));
            Assert.That(IoCManager.Resolve<IEntityManager>().HasComponent<PhysicsComponent>(ent1), Is.False);
            IEntity tempQualifier = entMan.GetEntity(grid.GridEntityId);
            Assert.That(IoCManager.Resolve<IEntityManager>().HasComponent<PhysicsComponent>(tempQualifier), Is.True);
        }

        /// <summary>
        /// Local position of an anchored entity cannot be changed (can still change world position via parent).
        /// Writing to the property is a no-op and is silently ignored.
        /// Because the position cannot be changed, MoveEvents are not raised when setting the property.
        /// </summary>
        [Test]
        public void Anchored_SetPosition_Nop()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            // coordinates are already tile centered to prevent snapping and MoveEvent
            var coordinates = new MapCoordinates(new Vector2(7.5f, 7.5f), TestMapId);

            // can only be anchored to a tile
            var grid = mapMan.GetGrid(TestGridId);
            grid.SetTile(grid.TileIndicesFor(coordinates), new Tile(1));

            var subscriber = new Subscriber();
            int calledCount = 0;
            var ent1 = entMan.SpawnEntity(null, coordinates); // this raises MoveEvent, subscribe after
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true; // Anchoring will change parent if needed, raising MoveEvent, subscribe after
            entMan.EventBus.SubscribeEvent<MoveEvent>(EventSource.Local, subscriber, MoveEventHandler);

            // Act
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).WorldPosition = new Vector2(99, 99);
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).LocalPosition = new Vector2(99, 99);
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Coordinates = new EntityCoordinates(grid.GridEntityId, 99, 99); // make sure not to change parent, that would un-anchor

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).MapPosition, Is.EqualTo(coordinates));
            Assert.That(calledCount, Is.EqualTo(0));
            void MoveEventHandler(ref MoveEvent ev)
            {
                Assert.Fail("MoveEvent raised when entity is anchored.");
                calledCount++;
            }
        }

        /// <summary>
        /// Changing the parent of the entity un-anchors it.
        /// </summary>
        [Test]
        public void Anchored_ChangeParent_Unanchors()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var coordinates = new MapCoordinates(new Vector2(7, 7), TestMapId);

            var grid = mapMan.GetGrid(TestGridId);

            var ent1 = entMan.SpawnEntity(null, coordinates);
            var tileIndices = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            // Act
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).ParentUid = mapMan.GetMapEntityId(TestMapId);

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored, Is.False);
            Assert.That(grid.GetAnchoredEntities(tileIndices).Count(), Is.EqualTo(0));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.EqualTo(new Tile(1)));
        }

        /// <summary>
        /// Setting the parent of an anchored entity to the same parent is a no-op (it will not be un-anchored).
        /// This is an specific case to the base functionality of TransformComponent, where in general setting the same
        /// parent is a no-op.
        /// </summary>
        [Test]
        public void Anchored_SetParentSame_Nop()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            // Act
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).ParentUid = grid.GridEntityId;

            Assert.That(grid.GetAnchoredEntities(tileIndices).First(), Is.EqualTo(ent1));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.Not.EqualTo(Tile.Empty));
        }

        /// <summary>
        /// If a tile is changed to a space tile, all entities anchored to that tile are unanchored.
        /// </summary>
        [Test]
        public void Anchored_TileToSpace_Unanchors()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            // Act
            grid.SetTile(tileIndices, Tile.Empty);

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored, Is.False);
            Assert.That(grid.GetAnchoredEntities(tileIndices).Count(), Is.EqualTo(0));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.EqualTo(Tile.Empty));
        }

        /// <summary>
        /// Adding an anchored entity to a container un-anchors an entity. There should be no way to have an anchored entity
        /// inside a container.
        /// </summary>
        /// <remarks>
        /// The only way you can do this without changing the parent is to make the parent grid a ContainerManager, then add the anchored entity to it.
        /// </remarks>
        [Test]
        public void Anchored_AddToContainer_Unanchors()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            // Act
            var gridEnt = entMan.GetEntity(grid.GridEntityId); // we purposefully use the grid as container so parent stays the same, reparent will unanchor
            var containerMan = IoCManager.Resolve<IEntityManager>().AddComponent<ContainerManagerComponent>(gridEnt);
            var container = containerMan.MakeContainer<Container>("TestContainer");
            container.Insert(ent1);

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored, Is.False);
            Assert.That(grid.GetAnchoredEntities(tileIndices).Count(), Is.EqualTo(0));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.EqualTo(new Tile(1)));
            Assert.That(container.ContainedEntities.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Adding a physics component should poll TransformComponent.Anchored for the correct body type.
        /// </summary>
        [Test]
        public void Anchored_AddPhysComp_IsStaticBody()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            // Act
            // assumed default body is Dynamic
            var physComp = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(ent1);

            Assert.That(physComp.BodyType, Is.EqualTo(BodyType.Static));
        }

        /// <summary>
        /// When an entity is anchored, it's physics body type is set to <see cref="BodyType.Static"/>.
        /// </summary>
        [Test]
        public void OnAnchored_HasPhysicsComp_IsStaticBody()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var coordinates = new MapCoordinates(new Vector2(7, 7), TestMapId);

            // can only be anchored to a tile
            var grid = mapMan.GetGrid(TestGridId);
            grid.SetTile(grid.TileIndicesFor(coordinates), new Tile(1));

            var ent1 = entMan.SpawnEntity(null, coordinates);
            var physComp = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(ent1);
            physComp.BodyType = BodyType.Dynamic;

            // Act
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            Assert.That(physComp.BodyType, Is.EqualTo(BodyType.Static));
        }

        /// <summary>
        /// When an entity is unanchored, it's physics body type is set to <see cref="BodyType.Dynamic"/>.
        /// </summary>
        [Test]
        public void OnUnanchored_HasPhysicsComp_IsDynamicBody()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            var physComp = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(ent1);
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            // Act
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = false;

            Assert.That(physComp.BodyType, Is.EqualTo(BodyType.Dynamic));
        }

        /// <summary>
        /// If an entity with an anchored prototype is spawned in an invalid location, the entity is unanchored.
        /// </summary>
        [Test]
        public void SpawnAnchored_EmptyTile_Unanchors()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);

            // Act
            var ent1 = entMan.SpawnEntity("anchoredEnt", new MapCoordinates(new Vector2(7, 7), TestMapId));

            var tileIndices = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Coordinates);
            Assert.That(grid.GetAnchoredEntities(tileIndices).Count(), Is.EqualTo(0));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.EqualTo(Tile.Empty));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored, Is.False);
        }

        /// <summary>
        /// If an entity is inside a container, setting Anchored silently fails.
        /// </summary>
        [Test]
        public void OnAnchored_InContainer_Nop()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Coordinates);
            grid.SetTile(tileIndices, new Tile(1));

            var gridEnt = entMan.GetEntity(grid.GridEntityId);
            var containerMan = IoCManager.Resolve<IEntityManager>().AddComponent<ContainerManagerComponent>(gridEnt);
            var container = containerMan.MakeContainer<Container>("TestContainer");
            container.Insert(ent1);

            // Act
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = true;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored, Is.False);
            Assert.That(grid.GetAnchoredEntities(tileIndices).Count(), Is.EqualTo(0));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.EqualTo(new Tile(1)));
            Assert.That(container.ContainedEntities.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Unanchoring an unanchored entity is a no-op.
        /// </summary>
        [Test]
        public void Unanchored_Unanchor_Nop()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var coordinates = new MapCoordinates(new Vector2(7, 7), TestMapId);

            // can only be anchored to a tile
            var grid = mapMan.GetGrid(TestGridId);
            grid.SetTile(grid.TileIndicesFor(coordinates), new Tile(1));

            var subscriber = new Subscriber();
            int calledCount = 0;
            var ent1 = entMan.SpawnEntity(null, coordinates); // this raises MoveEvent, subscribe after
            entMan.EventBus.SubscribeEvent<EntParentChangedMessage>(EventSource.Local, subscriber, ParentChangedHandler);

            // Act
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = false;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).ParentUid, Is.EqualTo(mapMan.GetMapEntityId(TestMapId)));
            Assert.That(calledCount, Is.EqualTo(0));
            void ParentChangedHandler(ref EntParentChangedMessage ev)
            {
                Assert.That(ev.Entity, Is.EqualTo(ent1));
                calledCount++;
            }
        }

        /// <summary>
        /// Unanchoring an entity should leave it parented to the grid it was anchored to.
        /// </summary>
        [Test]
        public void Anchored_Unanchored_ParentUnchanged()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var coordinates = new MapCoordinates(new Vector2(7, 7), TestMapId);

            // can only be anchored to a tile
            var grid = mapMan.GetGrid(TestGridId);
            grid.SetTile(grid.TileIndicesFor(coordinates), new Tile(1));
            var ent1 = entMan.SpawnEntity("anchoredEnt", grid.MapToGrid(coordinates));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).Anchored = false;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).ParentUid, Is.EqualTo(grid.GridEntityId));
        }
    }
}
