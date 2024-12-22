using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Reflection;
using Robust.UnitTesting.Server;


namespace Robust.UnitTesting.Shared.GameObjects.Systems
{
    [TestFixture, Parallelizable]
    public sealed partial class AnchoredSystemTests
    {
        private const string Prototypes = @"
- type: entity
  name: anchoredEnt
  id: anchoredEnt
  components:
  - type: Transform
    anchored: true";

        private static (ISimulation, Entity<MapGridComponent> grid, MapCoordinates, SharedTransformSystem xformSys, SharedMapSystem mapSys) SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterEntitySystems(f => f.LoadExtraSystemType<MoveEventTestSystem>())
                .RegisterPrototypes(f=>
                {
                    f.LoadString(Prototypes);
                })
                .InitializeInstance();

            var mapManager = sim.Resolve<IMapManager>();

            var testMapId = sim.CreateMap().MapId;
            var coords = new MapCoordinates(new Vector2(7, 7), testMapId);
            // Add grid 1, as the default grid to anchor things to.
            var grid = mapManager.CreateGridEntity(testMapId);

            return (sim, grid, coords, sim.System<SharedTransformSystem>(), sim.System<SharedMapSystem>());
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
            var (sim, grid, coordinates, xformSys, mapSys) = SimulationFactory();

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));

            var ent1 = sim.SpawnEntity(null, coordinates); // this raises MoveEvent, subscribe after

            // Act
            sim.System<MoveEventTestSystem>().ResetCounters();
            xformSys.AnchorEntity(ent1);
            Assert.That(xformSys.GetWorldPosition(ent1), Is.EqualTo(new Vector2(7.5f, 7.5f))); // centered on tile
            sim.System<MoveEventTestSystem>().AssertMoved(false);
        }

        [ComponentProtoName("AnchorOnInit")]
        [Reflect(false)]
        private sealed partial class AnchorOnInitComponent : Component;

        [Reflect(false)]
        private sealed class AnchorOnInitTestSystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();
                SubscribeLocalEvent<AnchorOnInitComponent, ComponentInit>((e, _, _) => Transform(e).Anchored = true);
            }
        }

        [Reflect(false)]
        internal sealed class MoveEventTestSystem : EntitySystem
        {
            [Dependency] private readonly SharedTransformSystem _transform = default!;

            public override void Initialize()
            {
                base.Initialize();
                _transform.OnGlobalMoveEvent += OnMove;
                SubscribeLocalEvent<EntParentChangedMessage>(OnReparent);
            }


            public override void Shutdown()
            {
                base.Shutdown();
                _transform.OnGlobalMoveEvent -= OnMove;
            }

            public bool FailOnMove;
            public int MoveCounter;
            public int ParentCounter;

            private void OnMove(ref MoveEvent ev)
            {
                MoveCounter++;
                if (FailOnMove)
                    Assert.Fail($"Move event was raised");
            }
            private void OnReparent(ref EntParentChangedMessage ev)
            {
                ParentCounter++;
                if (FailOnMove)
                    Assert.Fail($"Move event was raised");
            }

            public void ResetCounters()
            {
                ParentCounter = 0;
                MoveCounter = 0;
            }

            public void AssertMoved(bool parentChanged = true)
            {
                if (parentChanged)
                    Assert.That(ParentCounter, Is.EqualTo(1));
                Assert.That(MoveCounter, Is.EqualTo(1));
            }
        }

        /// <summary>
        ///     Ensures that if an entity gets added to lookups when anchored during init by some system.
        /// </summary>
        /// <remarks>
        ///     See space-wizards/RobustToolbox/issues/3444
        /// </remarks>
        [Test]
        public void OnInitAnchored_AddedToLookup()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterEntitySystems(f => f.LoadExtraSystemType<AnchorOnInitTestSystem>())
                .RegisterComponents(f => f.RegisterClass<AnchorOnInitComponent>())
                .InitializeInstance();

            var mapSys = sim.System<SharedMapSystem>();

            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = sim.CreateMap().MapId;
            var grid = mapMan.CreateGridEntity(mapId);
            var coordinates = new MapCoordinates(new Vector2(7, 7), mapId);
            var pos = mapSys.TileIndicesFor(grid, coordinates);
            mapSys.SetTile(grid, pos, new Tile(1));

            var ent1 = entMan.SpawnEntity(null, coordinates);
            Assert.That(sim.Transform(ent1).Anchored, Is.False);
            Assert.That(!mapSys.GetAnchoredEntities(grid, pos).Any());
            entMan.DeleteEntity(ent1);

            var ent2 = entMan.CreateEntityUninitialized(null, coordinates);
            entMan.AddComponent<AnchorOnInitComponent>(ent2);
            entMan.InitializeAndStartEntity(ent2);
            Assert.That(sim.Transform(ent2).Anchored);
            Assert.That(mapSys.GetAnchoredEntities(grid, pos).Count(), Is.EqualTo(1));
            Assert.That(mapSys.GetAnchoredEntities(grid, pos).Contains(ent2));
        }

        /// <summary>
        /// When an entity is anchored to a grid tile, it's parent is set to the grid.
        /// </summary>
        [Test]
        public void OnAnchored_Parent_SetToGrid()
        {
            var (sim, grid, coordinates, xformSys, mapSys) = SimulationFactory();

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));

            var traversal = sim.System<SharedGridTraversalSystem>();
            traversal.Enabled = false;
            var ent1 = sim.SpawnEntity(null, coordinates); // this raises MoveEvent, subscribe after

            // Act
            sim.System<MoveEventTestSystem>().ResetCounters();
            sim.Transform(ent1).Anchored = true;
            Assert.That(sim.Transform(ent1).ParentUid, Is.EqualTo(grid.Owner));
            sim.System<MoveEventTestSystem>().AssertMoved();
            traversal.Enabled = true;
        }

        /// <summary>
        /// Entities cannot be anchored to empty tiles. Attempting this is a no-op, and silently fails.
        /// </summary>
        [Test]
        public void OnAnchored_EmptyTile_Nop()
        {
            var (sim, grid, coords, xformSys, mapSys) = SimulationFactory();

            var ent1 = sim.SpawnEntity(null, coords);
            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1).Coordinates);
            mapSys.SetTile(grid, tileIndices, Tile.Empty);

            // Act
            xformSys.AnchorEntity(ent1);

            Assert.That(mapSys.GetAnchoredEntities(grid, tileIndices).Count(), Is.EqualTo(0));
            Assert.That(mapSys.GetTileRef(grid, tileIndices).Tile, Is.EqualTo(Tile.Empty));
        }

        /// <summary>
        /// Entities can be anchored to any non-empty grid tile. A physics component is not required on either
        /// the grid or the entity to anchor it.
        /// </summary>
        [Test]
        public void OnAnchored_NonEmptyTile_Anchors()
        {
            var (sim, grid, coords, xformSys, mapSys) = SimulationFactory();

            var ent1 = sim.SpawnEntity(null, coords);
            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1).Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));

            // Act
            sim.Transform(ent1).Anchored = true;

            Assert.That(mapSys.GetAnchoredEntities(grid, tileIndices).First(), Is.EqualTo(ent1));
            Assert.That(mapSys.GetTileRef(grid, tileIndices).Tile, Is.Not.EqualTo(Tile.Empty));
            Assert.That(sim.HasComp<PhysicsComponent>(ent1), Is.False);
            var tempQualifier = grid.Owner;
            Assert.That(sim.HasComp<PhysicsComponent>(tempQualifier), Is.True);
        }

        /// <summary>
        /// Local position of an anchored entity cannot be changed (can still change world position via parent).
        /// Writing to the property is a no-op and is silently ignored.
        /// Because the position cannot be changed, MoveEvents are not raised when setting the property.
        /// </summary>
        [Test]
        public void Anchored_SetPosition_Nop()
        {
            var (sim, grid, coordinates, xformSys, mapSys) = SimulationFactory();

            // coordinates are already tile centered to prevent snapping and MoveEvent
            coordinates = coordinates.Offset(new Vector2(0.5f, 0.5f));

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));

            var ent1 = sim.SpawnEntity(null, coordinates); // this raises MoveEvent, subscribe after
            sim.Transform(ent1).Anchored = true;
            sim.System<MoveEventTestSystem>().FailOnMove = true;

            // Act
            sim.Transform(ent1).WorldPosition = new Vector2(99, 99);
            sim.Transform(ent1).LocalPosition = new Vector2(99, 99);

            Assert.That(xformSys.GetMapCoordinates(ent1), Is.EqualTo(coordinates));
            sim.System<MoveEventTestSystem>().FailOnMove = false;
        }

        /// <summary>
        /// Changing the parent of the entity un-anchors it.
        /// </summary>
        [Test]
        public void Anchored_ChangeParent_Unanchors()
        {
            var (sim, grid, coordinates, xformSys, mapSys) = SimulationFactory();

            var ent1 = sim.SpawnEntity(null, coordinates);
            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1).Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));
            xformSys.AnchorEntity(ent1);

            // Act
            xformSys.SetParent(ent1, mapSys.GetMap(coordinates.MapId));

            Assert.That(sim.Transform(ent1).Anchored, Is.False);
            Assert.That(mapSys.GetAnchoredEntities(grid, tileIndices).Count(), Is.EqualTo(0));
            Assert.That(mapSys.GetTileRef(grid, tileIndices).Tile, Is.EqualTo(new Tile(1)));
        }

        /// <summary>
        /// Setting the parent of an anchored entity to the same parent is a no-op (it will not be un-anchored).
        /// This is an specific case to the base functionality of TransformComponent, where in general setting the same
        /// parent is a no-op.
        /// </summary>
        [Test]
        public void Anchored_SetParentSame_Nop()
        {
            var (sim, grid, coords, xformSys, mapSys) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();

            var ent1 = entMan.SpawnEntity(null, coords);
            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1).Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));
            sim.Transform(ent1).Anchored = true;

            // Act
            xformSys.SetParent(ent1, grid.Owner);

            Assert.That(mapSys.GetAnchoredEntities(grid, tileIndices).First(), Is.EqualTo(ent1));
            Assert.That(mapSys.GetTileRef(grid, tileIndices).Tile, Is.Not.EqualTo(Tile.Empty));
        }

        /// <summary>
        /// If a tile is changed to a space tile, all entities anchored to that tile are unanchored.
        /// </summary>
        [Test]
        public void Anchored_TileToSpace_Unanchors()
        {
            var (sim, grid, coords, xformSys, mapSys) = SimulationFactory();

            var ent1 = sim.SpawnEntity(null, coords);
            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1).Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));
            mapSys.SetTile(grid, new Vector2i(100, 100), new Tile(1)); // Prevents the grid from being deleted when the Act happens
            xformSys.AnchorEntity(ent1);

            // Act
            mapSys.SetTile(grid, tileIndices, Tile.Empty);

            Assert.That(sim.Transform(ent1).Anchored, Is.False);
            Assert.That(mapSys.GetAnchoredEntities(grid, tileIndices).Count(), Is.EqualTo(0));
            Assert.That(mapSys.GetTileRef(grid, tileIndices).Tile, Is.EqualTo(Tile.Empty));
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
            var (sim, grid, coords, xformSys, mapSys) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();

            var ent1 = sim.SpawnEntity(null, coords);
            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1).Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));
            xformSys.AnchorEntity(ent1);

            // Act
            // We purposefully use the grid as container so parent stays the same, reparent will unanchor
            var containerSys = entMan.System<SharedContainerSystem>();
            var containerMan = entMan.AddComponent<ContainerManagerComponent>(grid);
            var container = containerSys.MakeContainer<Container>(grid, "TestContainer", containerMan);
            containerSys.Insert(ent1, container);

            Assert.That(sim.Transform(ent1).Anchored, Is.False);
            Assert.That(mapSys.GetAnchoredEntities(grid, tileIndices).Count(), Is.EqualTo(0));
            Assert.That(mapSys.GetTileRef(grid, tileIndices).Tile, Is.EqualTo(new Tile(1)));
            Assert.That(container.ContainedEntities.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Adding a physics component should poll TransformComponent.Anchored for the correct body type.
        /// </summary>
        [Test]
        public void Anchored_AddPhysComp_IsStaticBody()
        {
            var (sim, grid, coords, xformSys, mapSys) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();

            var ent1 = sim.SpawnEntity(null, coords);
            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1).Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));
            xformSys.AnchorEntity(ent1);

            // Act
            // assumed default body is Dynamic
            var physComp = entMan.AddComponent<PhysicsComponent>(ent1);

            Assert.That(physComp.BodyType, Is.EqualTo(BodyType.Static));
        }

        /// <summary>
        /// When an entity is anchored, it's physics body type is set to <see cref="BodyType.Static"/>.
        /// </summary>
        [Test]
        public void OnAnchored_HasPhysicsComp_IsStaticBody()
        {
            var (sim, grid, coordinates, xformSys, mapSys) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var physSystem = sim.System<SharedPhysicsSystem>();

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));

            var ent1 = entMan.SpawnEntity(null, coordinates);
            var physComp = entMan.AddComponent<PhysicsComponent>(ent1);
            physSystem.SetBodyType(ent1, BodyType.Dynamic, body: physComp);

            // Act
            xformSys.AnchorEntity(ent1);

            Assert.That(physComp.BodyType, Is.EqualTo(BodyType.Static));
        }

        /// <summary>
        /// When an entity is unanchored, it's physics body type is set to <see cref="BodyType.Dynamic"/>.
        /// </summary>
        [Test]
        public void OnUnanchored_HasPhysicsComp_IsDynamicBody()
        {
            var (sim, grid, coords, xformSys, mapSys) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();

            var ent1 = sim.SpawnEntity(null, coords);
            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1).Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));
            var physComp = entMan.AddComponent<PhysicsComponent>(ent1);
            sim.Transform(ent1).Anchored = true;

            // Act
            xformSys.Unanchor(ent1);

            Assert.That(physComp.BodyType, Is.EqualTo(BodyType.Dynamic));
        }

        /// <summary>
        /// If an entity with an anchored prototype is spawned in an invalid location, the entity is unanchored.
        /// </summary>
        [Test]
        public void SpawnAnchored_EmptyTile_Unanchors()
        {
            var (sim, grid, coords, _, mapSys) = SimulationFactory();

            // Act
            var ent1 = sim.SpawnEntity("anchoredEnt", coords);

            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1).Coordinates);
            Assert.That(mapSys.GetAnchoredEntities(grid, tileIndices).Count(), Is.EqualTo(0));
            Assert.That(mapSys.GetTileRef(grid, tileIndices).Tile, Is.EqualTo(Tile.Empty));
            Assert.That(sim.Transform(ent1).Anchored, Is.False);
        }

        /// <summary>
        /// If an entity is inside a container, setting Anchored silently fails.
        /// </summary>
        [Test]
        public void OnAnchored_InContainer_Nop()
        {
            var (sim, grid, coords, xformSys, mapSys) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();

            var ent1 = sim.SpawnEntity(null, coords);
            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1).Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));

            var containerSys = entMan.System<SharedContainerSystem>();
            var containerMan = entMan.AddComponent<ContainerManagerComponent>(grid);
            var container = containerSys.MakeContainer<Container>(grid, "TestContainer", containerMan);
            containerSys.Insert(ent1, container);

            // Act
            xformSys.AnchorEntity(ent1);

            Assert.That(sim.Transform(ent1).Anchored, Is.False);
            Assert.That(mapSys.GetAnchoredEntities(grid, tileIndices).Count(), Is.EqualTo(0));
            Assert.That(mapSys.GetTileRef(grid, tileIndices).Tile, Is.EqualTo(new Tile(1)));
            Assert.That(container.ContainedEntities.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Unanchoring an unanchored entity is a no-op.
        /// </summary>
        [Test]
        public void Unanchored_Unanchor_Nop()
        {
            var (sim, grid, coordinates, xformSys, mapSys) = SimulationFactory();

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));

            var traversal = sim.System<SharedGridTraversalSystem>();
            traversal.Enabled = false;
            var ent1 = sim.SpawnEntity(null, coordinates); // this raises MoveEvent, subscribe after

            // Act
            sim.System<MoveEventTestSystem>().FailOnMove = true;
            xformSys.Unanchor(ent1);
            Assert.That(sim.Transform(ent1).ParentUid, Is.EqualTo(mapSys.GetMap(coordinates.MapId)));
            sim.System<MoveEventTestSystem>().FailOnMove = false;
            traversal.Enabled = true;
        }

        /// <summary>
        /// Unanchoring an entity should leave it parented to the grid it was anchored to.
        /// </summary>
        [Test]
        public void Anchored_Unanchored_ParentUnchanged()
        {
            var (sim, grid, coordinates, xformSys, mapSys) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));
            var ent1 = entMan.SpawnEntity("anchoredEnt", mapSys.MapToGrid(grid, coordinates));

            xformSys.Unanchor(ent1);

            Assert.That(sim.Transform(ent1).ParentUid, Is.EqualTo(grid.Owner));
        }
    }
}
