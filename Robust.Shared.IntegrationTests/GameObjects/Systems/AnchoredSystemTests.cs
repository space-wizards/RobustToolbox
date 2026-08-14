using System.Collections.Generic;
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
    internal sealed partial class AnchoredSystemTests
    {
        private const string AnchoredProto = "anchoredEnt";

        private const string Prototypes = $@"
- type: entity
  name: anchoredEnt
  id: {AnchoredProto}
  components:
  - type: Transform
    anchored: true";

        private static (ISimulation, Entity<MapGridComponent> grid, MapCoordinates, SharedTransformSystem xformSys, SharedMapSystem mapSys, IEntityManager entMan) SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterEntitySystems(f => f.LoadExtraSystemType<MoveEventTestSystem>())
                .RegisterPrototypes(f=>
                {
                    f.LoadString(Prototypes);
                })
                .InitializeInstance();

            var entManager = sim.Resolve<IEntityManager>();

            var mapSystem = entManager.System<SharedMapSystem>();

            mapSystem.CreateMap(out var testMapId);
            var coords = new MapCoordinates(new Vector2(7, 7), testMapId);
            // Add grid 1, as the default grid to anchor things to.
            var grid = mapSystem.CreateGridEntity(testMapId);

            return (sim, grid, coords, entManager.System<SharedTransformSystem>(), mapSystem, entManager);
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
            var (sim, grid, coordinates, xformSys, mapSys, entMan) = SimulationFactory();

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));

            var ent1 = entMan.Spawn(null, coordinates); // this raises MoveEvent, subscribe after

            // Act
            var moveEventTest = sim.System<MoveEventTestSystem>();
            moveEventTest.ResetCounters();
            xformSys.AnchorEntity((ent1, sim.Transform(ent1, entMan)), grid, mapSys.TileIndicesFor(grid, coordinates));
            Assert.That(xformSys.GetWorldPosition(ent1), Is.EqualTo(new Vector2(7.5f, 7.5f))); // centered on tile
            moveEventTest.AssertMoved(false);
        }

        [ComponentProtoName("AnchorOnInit")]
        [Reflect(false)]
        private sealed partial class AnchorOnInitComponent : Component;

        [Reflect(false)]
        private sealed partial class AnchorOnInitTestSystem : EntitySystem
        {
            [Dependency] private SharedTransformSystem _transform = default!;

            public override void Initialize()
            {
                base.Initialize();
                SubscribeLocalEvent<AnchorOnInitComponent, ComponentInit>((e, _, _) => _transform.AnchorEntity((e, Transform(e))));
            }
        }

        [Reflect(false)]
        internal sealed partial class MoveEventTestSystem : EntitySystem
        {
            [Dependency] private SharedTransformSystem _transform = default!;

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
                    Assert.Fail("Move event was raised");
            }
            private void OnReparent(ref EntParentChangedMessage ev)
            {
                ParentCounter++;
                if (FailOnMove)
                    Assert.Fail("Move event was raised");
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

            var entMan = sim.Resolve<IEntityManager>();
            var mapSys = entMan.System<SharedMapSystem>();

            mapSys.CreateMap(out var mapId);
            var grid = mapSys.CreateGridEntity(mapId);
            var coordinates = new MapCoordinates(new Vector2(7, 7), mapId);
            var pos = mapSys.TileIndicesFor(grid, coordinates);
            mapSys.SetTile(grid, pos, new Tile(1));

            var ent1 = entMan.SpawnEntity(null, coordinates);
            Assert.That(sim.Transform(ent1, entMan).Anchored, Is.False);
            Assert.That(!mapSys.GetAnchoredEntities(grid, pos).Any());
            entMan.DeleteEntity(ent1);

            var ent2 = entMan.CreateEntityUninitialized(null, coordinates);
            entMan.AddComponent<AnchorOnInitComponent>(ent2);
            entMan.InitializeAndStartEntity(ent2);
            Assert.That(sim.Transform(ent2, entMan).Anchored);
            Assert.That(mapSys.GetAnchoredEntities(grid, pos).Count(), Is.EqualTo(1));
            Assert.That(mapSys.GetAnchoredEntities(grid, pos).Contains(ent2));
        }

        /// <summary>
        /// When an entity is anchored to a grid tile, it's parent is set to the grid.
        /// </summary>
        [Test]
        public void OnAnchored_Parent_SetToGrid()
        {
            var (sim, grid, coordinates, xformSys, mapSys, entMan) = SimulationFactory();

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));

            var traversal = entMan.System<SharedGridTraversalSystem>();
            traversal.Enabled = false;
            var ent1 = entMan.Spawn(null, coordinates); // this raises MoveEvent, subscribe after

            // Act
            xformSys.AnchorEntity(ent1);
            Assert.That(sim.Transform(ent1, entMan).ParentUid, Is.EqualTo(grid.Owner));
            traversal.Enabled = true;
        }

        /// <summary>
        /// Entities cannot be anchored to empty tiles. Attempting this is a no-op, and silently fails.
        /// </summary>
        [Test]
        public void OnAnchored_EmptyTile_Nop()
        {
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();

            var ent1 = entMan.Spawn(null, coords);
            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1, entMan).Coordinates);
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
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();

            var ent1 = entMan.Spawn(null, coords);
            var xform = sim.Transform(ent1, entMan);
            var tileIndices = mapSys.TileIndicesFor(grid, xform.Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));

            // Act
            xformSys.AnchorEntity(ent1);

            Assert.That(mapSys.GetAnchoredEntities(grid, tileIndices).First(), Is.EqualTo(ent1));
            Assert.That(mapSys.GetTileRef(grid, tileIndices).Tile, Is.Not.EqualTo(Tile.Empty));
            Assert.That(sim.HasComp<PhysicsComponent>(ent1, entMan), Is.False);
            var tempQualifier = grid.Owner;
            Assert.That(sim.HasComp<PhysicsComponent>(tempQualifier, entMan), Is.True);
        }

        // Acruid-style names

        /*
         * Assert same-spot anchoring works.
         */

        [Test]
        public void AnchorEntity_CurrentMapPosition_AnchorsToGridUnderEntity()
        {
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();
            var mapUid = mapSys.GetMap(coords.MapId);
            var tileIndices = mapSys.TileIndicesFor(grid, coords);
            mapSys.SetTile(grid, tileIndices, new Tile(1));

            var ent1 = entMan.SpawnEntity(null, new EntityCoordinates(mapUid, coords.Position));

            Assert.That(xformSys.AnchorEntity(ent1), Is.True);
            Assert.That(sim.Transform(ent1, entMan).Anchored, Is.True);
            Assert.That(sim.Transform(ent1, entMan).ParentUid, Is.EqualTo(grid.Owner));
            Assert.That(mapSys.GetAnchoredEntities(grid, tileIndices), Does.Contain(ent1));
            Assert.That(xformSys.GetWorldPosition(ent1), Is.EqualTo(new Vector2(7f, 7f)));
        }

        [Test]
        public void AnchorEntity_GridTile_ReanchorsAndCleansOldTile()
        {
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();
            var oldTile = mapSys.TileIndicesFor(grid, coords);
            var newTile = oldTile + new Vector2i(1, 0);
            mapSys.SetTile(grid, oldTile, new Tile(1));
            mapSys.SetTile(grid, newTile, new Tile(1));

            var ent1 = entMan.SpawnEntity(null, coords);
            Assert.That(xformSys.AnchorEntity(ent1), Is.True);

            Assert.That(xformSys.AnchorEntity((ent1, sim.Transform(ent1, entMan)), grid, newTile), Is.True);

            Assert.That(mapSys.GetAnchoredEntities(grid, oldTile), Does.Not.Contain(ent1));
            Assert.That(mapSys.GetAnchoredEntities(grid, newTile), Does.Contain(ent1));
            Assert.That(xformSys.GetWorldPosition(ent1), Is.EqualTo(new Vector2(8.5f, 7.5f)));
        }

        [Test]
        public void AnchorEntity_GridTile_SameTile_Noops()
        {
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();
            var tile = mapSys.TileIndicesFor(grid, coords);
            mapSys.SetTile(grid, tile, new Tile(1));

            var ent1 = entMan.SpawnEntity(null, coords);
            Assert.That(xformSys.AnchorEntity(ent1), Is.True);

            // I LOVE THIS SYSTEM
            sim.System<MoveEventTestSystem>().ResetCounters();
            Assert.That(xformSys.AnchorEntity((ent1, sim.Transform(ent1, entMan)), grid, tile), Is.True);

            Assert.That(sim.Transform(ent1, entMan).Anchored, Is.True);
            Assert.That(mapSys.GetAnchoredEntities(grid, tile), Does.Contain(ent1));
            Assert.That(sim.System<MoveEventTestSystem>().MoveCounter, Is.EqualTo(0));
            Assert.That(sim.System<MoveEventTestSystem>().ParentCounter, Is.EqualTo(0));
        }

        [Test]
        public void AnchorEntity_GridTile_InvalidReanchor_MovesAndUnanchors()
        {
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();
            var oldTile = mapSys.TileIndicesFor(grid, coords);
            var emptyTile = oldTile + new Vector2i(1, 0);
            mapSys.SetTile(grid, oldTile, new Tile(1));
            mapSys.SetTile(grid, emptyTile, Tile.Empty);

            var ent1 = entMan.SpawnEntity(null, coords);
            Assert.That(xformSys.AnchorEntity(ent1), Is.True);

            Assert.That(xformSys.AnchorEntity((ent1, sim.Transform(ent1, entMan)), grid, emptyTile), Is.False);

            Assert.That(sim.Transform(ent1, entMan).Anchored, Is.False);
            Assert.That(mapSys.GetAnchoredEntities(grid, oldTile), Does.Not.Contain(ent1));
            Assert.That(mapSys.GetAnchoredEntities(grid, emptyTile), Does.Not.Contain(ent1));
            Assert.That(xformSys.GetWorldPosition(ent1), Is.EqualTo(new Vector2(8.5f, 7.5f)));
        }

        [Test]
        public void AnchorEntity_GridTile_StaleSameTileAnchor_RepairsLookup()
        {
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();
            var tile = mapSys.TileIndicesFor(grid, coords);
            mapSys.SetTile(grid, tile, new Tile(1));

            var ent1 = entMan.SpawnEntity(null, coords);
            Assert.That(xformSys.AnchorEntity(ent1), Is.True);
            mapSys.RemoveFromSnapGridCell(grid, grid.Comp, tile, ent1);

            Assert.That(xformSys.AnchorEntity((ent1, sim.Transform(ent1, entMan)), grid, tile), Is.True);

            Assert.That(sim.Transform(ent1, entMan).Anchored, Is.True);
            Assert.That(mapSys.GetAnchoredEntities(grid, tile), Does.Contain(ent1));
        }

        [Test]
        public void AnchorEntity_TileRef_MovesAndAnchors()
        {
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();
            var targetTile = mapSys.TileIndicesFor(grid, coords) + new Vector2i(1, 0);
            mapSys.SetTile(grid, targetTile, new Tile(1));
            var tileRef = mapSys.GetTileRef(grid, targetTile);

            var ent1 = entMan.SpawnEntity(null, coords);

            Assert.That(xformSys.AnchorEntity((ent1, sim.Transform(ent1, entMan)), tileRef), Is.True);

            Assert.That(sim.Transform(ent1, entMan).Anchored, Is.True);
            Assert.That(mapSys.GetAnchoredEntities(grid, targetTile), Does.Contain(ent1));
            Assert.That(xformSys.GetWorldPosition(ent1), Is.EqualTo(new Vector2(8.5f, 7.5f)));
        }

        [Test]
        public void AnchorEntity_MapCoordinates_MovesEvenWhenAnchorFails()
        {
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();
            var targetCoords = coords.Offset(new Vector2(1, 0));
            var targetTile = mapSys.TileIndicesFor(grid, targetCoords);
            mapSys.SetTile(grid, targetTile, Tile.Empty);

            var ent1 = entMan.SpawnEntity(null, coords);

            Assert.That(xformSys.AnchorEntity((ent1, sim.Transform(ent1, entMan)), targetCoords), Is.False);

            Assert.That(sim.Transform(ent1, entMan).Anchored, Is.False);
            Assert.That(mapSys.GetAnchoredEntities(grid, targetTile), Does.Not.Contain(ent1));
            Assert.That(xformSys.GetWorldPosition(ent1), Is.EqualTo(targetCoords.Position));
        }

        /*
         * Down here we test the targeted variants and not same-spot anchoring.
         */

        internal enum AnchorTargetVariant : byte
        {
            GridTile,
            TileRef,
            EntityCoordinates,
            MapCoordinates,
        }

        internal enum AnchorTargetScenario : byte
        {
            SameSpot,
            DifferentGrid,
            NoGrid,
        }

        private static IEnumerable<TestCaseData> AnchorTargetCases()
        {
            foreach (var variant in new[]
                     {
                         AnchorTargetVariant.GridTile,
                         AnchorTargetVariant.TileRef,
                         AnchorTargetVariant.EntityCoordinates,
                         AnchorTargetVariant.MapCoordinates,
                     })
            {
                yield return new TestCaseData(variant, AnchorTargetScenario.SameSpot)
                    .SetName($"AnchorEntity_TargetedVariants_SameSpot_{variant}");
                yield return new TestCaseData(variant, AnchorTargetScenario.DifferentGrid)
                    .SetName($"AnchorEntity_TargetedVariants_DifferentGrid_{variant}");
            }

            yield return new TestCaseData(AnchorTargetVariant.EntityCoordinates, AnchorTargetScenario.NoGrid)
                .SetName("AnchorEntity_TargetedVariants_NoGrid_EntityCoordinates");
            yield return new TestCaseData(AnchorTargetVariant.MapCoordinates, AnchorTargetScenario.NoGrid)
                .SetName("AnchorEntity_TargetedVariants_NoGrid_MapCoordinates");
        }

        [TestCaseSource(nameof(AnchorTargetCases))]
        public void AnchorEntity_TargetedVariants_Scenarios(AnchorTargetVariant variant, AnchorTargetScenario scenario)
        {
            var (sim, gridA, coords, xformSys, mapSys, entMan) = SimulationFactory();
            var mapUid = mapSys.GetMap(coords.MapId);
            var gridB = mapSys.CreateGridEntity(coords.MapId);

            var tileA = mapSys.TileIndicesFor(gridA, coords);
            var tileB = new Vector2i(10, 0);
            mapSys.SetTile(gridA, tileA, new Tile(1));
            mapSys.SetTile(gridB, tileB, new Tile(1));

            var ent = entMan.SpawnEntity(null, coords);
            var xform = sim.Transform(ent, entMan);
            Assert.That(xformSys.AnchorEntity(ent), Is.True);

            sim.System<MoveEventTestSystem>().ResetCounters();

            var result = variant switch
            {
                AnchorTargetVariant.GridTile => xformSys.AnchorEntity((ent, xform), TargetGrid(), TargetTile()),
                AnchorTargetVariant.TileRef => xformSys.AnchorEntity((ent, xform), mapSys.GetTileRef(TargetGrid(), TargetTile())),
                AnchorTargetVariant.EntityCoordinates => xformSys.AnchorEntity((ent, xform), TargetCoordinates()),
                AnchorTargetVariant.MapCoordinates => xformSys.AnchorEntity((ent, xform), TargetMapCoordinates()),
                _ => throw new System.ArgumentOutOfRangeException(nameof(variant), variant, null),
            };

            switch (scenario)
            {
                case AnchorTargetScenario.SameSpot:
                    Assert.That(result, Is.True);
                    Assert.That(xform.Anchored, Is.True);
                    Assert.That(mapSys.GetAnchoredEntities(gridA, tileA), Does.Contain(ent));
                    Assert.That(sim.System<MoveEventTestSystem>().MoveCounter, Is.EqualTo(0));
                    Assert.That(sim.System<MoveEventTestSystem>().ParentCounter, Is.EqualTo(0));
                    break;
                case AnchorTargetScenario.DifferentGrid:
                    Assert.That(result, Is.True);
                    Assert.That(xform.Anchored, Is.True);
                    Assert.That(mapSys.GetAnchoredEntities(gridA, tileA), Does.Not.Contain(ent));
                    Assert.That(mapSys.GetAnchoredEntities(gridB, tileB), Does.Contain(ent));
                    Assert.That(xformSys.GetWorldPosition(ent), Is.EqualTo(new Vector2(10.5f, 0.5f)));
                    break;
                case AnchorTargetScenario.NoGrid:
                    Assert.That(result, Is.False);
                    Assert.That(xform.Anchored, Is.False);
                    Assert.That(mapSys.GetAnchoredEntities(gridA, tileA), Does.Not.Contain(ent));
                    Assert.That(xformSys.GetWorldPosition(ent), Is.EqualTo(new Vector2(1000, 1000)));
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }

            Entity<MapGridComponent> TargetGrid()
            {
                return scenario == AnchorTargetScenario.DifferentGrid ? gridB : gridA;
            }

            Vector2i TargetTile()
            {
                return scenario == AnchorTargetScenario.DifferentGrid ? tileB : tileA;
            }

            EntityCoordinates TargetCoordinates()
            {
                return scenario switch
                {
                    AnchorTargetScenario.DifferentGrid => new EntityCoordinates(gridB, 10.5f, 0.5f),
                    AnchorTargetScenario.NoGrid => new EntityCoordinates(mapUid, 1000, 1000),
                    _ => new EntityCoordinates(gridA, 7.5f, 7.5f),
                };
            }

            MapCoordinates TargetMapCoordinates()
            {
                return scenario switch
                {
                    AnchorTargetScenario.DifferentGrid => new MapCoordinates(new Vector2(10.5f, 0.5f), coords.MapId),
                    AnchorTargetScenario.NoGrid => new MapCoordinates(new Vector2(1000, 1000), coords.MapId),
                    _ => new MapCoordinates(new Vector2(7.5f, 7.5f), coords.MapId),
                };
            }
        }

        internal enum UnanchorVariant : byte
        {
            Uid,
            TryAnchor,
        }

        internal enum UnanchorScenario : byte
        {
            SameSpot,
            DifferentGrid,
            NoGrid,
        }

        private static IEnumerable<TestCaseData> UnanchorCases()
        {
            foreach (var variant in new[] {UnanchorVariant.Uid, UnanchorVariant.TryAnchor})
            {
                yield return new TestCaseData(variant, UnanchorScenario.SameSpot)
                    .SetName($"UnanchorEntity_Variants_SameSpot_{variant}");
                yield return new TestCaseData(variant, UnanchorScenario.DifferentGrid)
                    .SetName($"UnanchorEntity_Variants_DifferentGrid_{variant}");
                yield return new TestCaseData(variant, UnanchorScenario.NoGrid)
                    .SetName($"UnanchorEntity_Variants_NoGrid_{variant}");
            }
        }

        [TestCaseSource(nameof(UnanchorCases))]
        public void UnanchorEntity_Variants_Scenarios(UnanchorVariant variant, UnanchorScenario scenario)
        {
            var (sim, gridA, coords, xformSys, mapSys, entMan) = SimulationFactory();
            var mapUid = mapSys.GetMap(coords.MapId);
            var gridB = mapSys.CreateGridEntity(coords.MapId);

            var tileA = mapSys.TileIndicesFor(gridA, coords);
            var tileB = tileA + new Vector2i(10, 0);
            mapSys.SetTile(gridA, tileA, new Tile(1));
            mapSys.SetTile(gridB, tileB, new Tile(1));

            var ent = entMan.SpawnEntity(null, coords);
            var xform = sim.Transform(ent, entMan);
            var meta = entMan.GetComponent<MetaDataComponent>(ent);

            switch (scenario)
            {
                case UnanchorScenario.SameSpot:
                    Assert.That(xformSys.AnchorEntity(ent), Is.True);
                    break;
                case UnanchorScenario.DifferentGrid:
                    Assert.That(xformSys.AnchorEntity((ent, xform), gridB, tileB), Is.True);
                    break;
                case UnanchorScenario.NoGrid:
                    xformSys.SetCoordinates((ent, xform, meta), new EntityCoordinates(mapUid, 1000, 1000));
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }

            var result = variant switch
            {
                UnanchorVariant.Uid => xformSys.Unanchor(ent),
                UnanchorVariant.TryAnchor => xformSys.TryAnchor((ent, xform, meta), false),
                _ => throw new System.ArgumentOutOfRangeException(nameof(variant), variant, null),
            };

            Assert.That(result, Is.False);
            Assert.That(xform.Anchored, Is.False);
            Assert.That(mapSys.GetAnchoredEntities(gridA, tileA), Does.Not.Contain(ent));
            Assert.That(mapSys.GetAnchoredEntities(gridB, tileB), Does.Not.Contain(ent));
        }

        /// <summary>
        /// Local position of an anchored entity cannot be changed (can still change world position via parent).
        /// Writing to the property is a no-op and is silently ignored.
        /// Because the position cannot be changed, MoveEvents are not raised when setting the property.
        /// </summary>
        [Test]
        public void Anchored_SetPosition_Nop()
        {
            var (sim, grid, coordinates, xformSys, mapSys, entMan) = SimulationFactory();

            // coordinates are already tile centered to prevent snapping and MoveEvent
            coordinates = coordinates.Offset(new Vector2(0.5f, 0.5f));

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));

            var ent1 = entMan.SpawnEntity(null, coordinates); // this raises MoveEvent, subscribe after
            var xform = sim.Transform(ent1, entMan);
            xformSys.AnchorEntity(ent1);
            sim.System<MoveEventTestSystem>().FailOnMove = true;

            // Act
#pragma warning disable CS0618 // Checking property setters.
            xform.WorldPosition = new Vector2(99, 99);
            xform.LocalPosition = new Vector2(99, 99);
#pragma warning restore CS0618

            Assert.That(xformSys.GetMapCoordinates(ent1), Is.EqualTo(coordinates));
            var moveEventTest = sim.System<MoveEventTestSystem>();
            moveEventTest.FailOnMove = false;
        }

        /// <summary>
        /// Changing the parent of the entity un-anchors it.
        /// </summary>
        [Test]
        public void Anchored_ChangeParent_Unanchors()
        {
            var (sim, grid, coordinates, xformSys, mapSys, entMan) = SimulationFactory();

            var ent1 = entMan.Spawn(null, coordinates);
            var xform = sim.Transform(ent1, entMan);
            var tileIndices = mapSys.TileIndicesFor(grid, xform.Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));
            xformSys.AnchorEntity(ent1);

            // Act
            xformSys.SetParent(ent1, mapSys.GetMap(coordinates.MapId));

            Assert.That(xform.Anchored, Is.False);
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
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();

            var ent1 = entMan.Spawn(null, coords);
            var xform = sim.Transform(ent1, entMan);
            var tileIndices = mapSys.TileIndicesFor(grid, xform.Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));
            xformSys.AnchorEntity(ent1);

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
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();

            var ent1 = entMan.Spawn(null, coords);
            var xform = sim.Transform(ent1, entMan);
            var tileIndices = mapSys.TileIndicesFor(grid, xform.Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));
            mapSys.SetTile(grid, new Vector2i(100, 100), new Tile(1)); // Prevents the grid from being deleted when the Act happens
            xformSys.AnchorEntity(ent1);

            // Act
            mapSys.SetTile(grid, tileIndices, Tile.Empty);

            Assert.That(xform.Anchored, Is.False);
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
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();

            var ent1 = entMan.Spawn(null, coords);
            var xform = sim.Transform(ent1, entMan);
            var tileIndices = mapSys.TileIndicesFor(grid, xform.Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));
            xformSys.AnchorEntity(ent1);

            // Act
            // We purposefully use the grid as container so parent stays the same, reparent will unanchor
            var containerSys = entMan.System<SharedContainerSystem>();
            var containerMan = entMan.AddComponent<ContainerManagerComponent>(grid);
            var container = containerSys.MakeContainer<Container>(grid, "TestContainer", containerMan);
            containerSys.Insert(ent1, container);

            Assert.That(xform.Anchored, Is.False);
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
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();

            var ent1 = entMan.Spawn(null, coords);
            var tileIndices = mapSys.TileIndicesFor(grid, sim.Transform(ent1, entMan).Coordinates);
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
            var (sim, grid, coordinates, xformSys, mapSys, entMan) = SimulationFactory();
            var physSystem = entMan.System<SharedPhysicsSystem>();

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));

            var ent1 = entMan.Spawn(null, coordinates);
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
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();

            var ent1 = entMan.Spawn(null, coords);
            var xform = sim.Transform(ent1, entMan);
            var tileIndices = mapSys.TileIndicesFor(grid, xform.Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));
            var physComp = entMan.AddComponent<PhysicsComponent>(ent1);
            xformSys.AnchorEntity(ent1);

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
            var (sim, grid, coords, _, mapSys, entMan) = SimulationFactory();

            // Act
            var ent1 = entMan.Spawn(AnchoredProto, coords);
            var xform = sim.Transform(ent1, entMan);

            var tileIndices = mapSys.TileIndicesFor(grid, xform.Coordinates);
            Assert.That(mapSys.GetAnchoredEntities(grid, tileIndices).Count(), Is.EqualTo(0));
            Assert.That(mapSys.GetTileRef(grid, tileIndices).Tile, Is.EqualTo(Tile.Empty));
            Assert.That(xform.Anchored, Is.False);
        }

        /// <summary>
        /// If an entity is inside a container, setting Anchored silently fails.
        /// </summary>
        [Test]
        public void OnAnchored_InContainer_Nop()
        {
            var (sim, grid, coords, xformSys, mapSys, entMan) = SimulationFactory();

            var ent1 = entMan.Spawn(null, coords);
            var xform = sim.Transform(ent1, entMan);
            var tileIndices = mapSys.TileIndicesFor(grid, xform.Coordinates);
            mapSys.SetTile(grid, tileIndices, new Tile(1));

            var containerSys = entMan.System<SharedContainerSystem>();
            var containerMan = entMan.AddComponent<ContainerManagerComponent>(grid);
            var container = containerSys.MakeContainer<Container>(grid, "TestContainer", containerMan);
            containerSys.Insert(ent1, container);

            // Act
            xformSys.AnchorEntity(ent1);

            Assert.That(xform.Anchored, Is.False);
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
            var (sim, grid, coordinates, xformSys, mapSys, entMan) = SimulationFactory();

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));

            var traversal = entMan.System<SharedGridTraversalSystem>();
            traversal.Enabled = false;
            var ent1 = entMan.Spawn(null, coordinates); // this raises MoveEvent, subscribe after

            // Act
            var moveEventTest = entMan.System<MoveEventTestSystem>();
            moveEventTest.FailOnMove = true;
            xformSys.Unanchor(ent1);
            Assert.That(sim.Transform(ent1, entMan).ParentUid, Is.EqualTo(grid.Owner));
            moveEventTest.FailOnMove = false;
            traversal.Enabled = true;
        }

        /// <summary>
        /// Unanchoring an entity should leave it parented to the grid it was anchored to.
        /// </summary>
        [Test]
        public void Anchored_Unanchored_ParentUnchanged()
        {
            var (sim, grid, coordinates, xformSys, mapSys, entMan) = SimulationFactory();

            // can only be anchored to a tile
            mapSys.SetTile(grid, mapSys.TileIndicesFor(grid, coordinates), new Tile(1));
            var ent1 = entMan.SpawnAttachedTo(AnchoredProto, mapSys.MapToGrid(grid, coordinates));

            xformSys.Unanchor(ent1);

            Assert.That(sim.Transform(ent1, entMan).ParentUid, Is.EqualTo(grid.Owner));
        }
    }
}
