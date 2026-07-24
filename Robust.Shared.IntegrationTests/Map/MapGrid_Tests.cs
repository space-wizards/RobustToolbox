using System.Linq;
using System.Numerics;
using Moq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, TestOf(typeof(MapGridComponent))]
    sealed class MapGrid_Tests
    {
        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .InitializeInstance();

            return sim;
        }

        [Test]
        public void GetTileRefCoords()
        {
            var sim = SimulationFactory();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapSystem.CreateGridEntity(mapId, gridOptions);

            mapSystem.SetTile(grid, new Vector2i(-9, -1), new Tile(typeId: 1, flags: 1, variant: 1));
            var result = mapSystem.GetTileRef(grid.Owner, grid.Comp, new Vector2i(-9, -1));

            Assert.That(grid.Comp.ChunkCount, Is.EqualTo(1));
            Assert.That(mapSystem.GetMapChunks(grid.Owner, grid.Comp).Keys.ToList()[0], Is.EqualTo(new Vector2i(-2, -1)));
            Assert.That(result, Is.EqualTo(new TileRef(grid.Owner, new Vector2i(-9,-1), new Tile(typeId: 1, flags: 1, variant: 1))));
        }

        [TestCaseSource(nameof(MapEnumeratorCases))]
        public void MapEnumeratorsHandleNonEmptyResults(Func<MapEnumeratorTestContext, bool> containsExpected)
        {
            var ctx = SetupMapEnumeratorTest();
            Assert.That(containsExpected(ctx), Is.True);
        }

        [TestCaseSource(nameof(TileEnumeratorCases))]
        public void TileEnumeratorsHandleNonEmptyResults(Func<MapEnumeratorTestContext, bool> containsExpected)
        {
            var ctx = SetupMapEnumeratorTest();
            Assert.That(containsExpected(ctx), Is.True);
        }

        [TestCaseSource(nameof(AnchoredEnumeratorCases))]
        public void AnchoredEnumeratorsHandleNonEmptyResults(Func<MapEnumeratorTestContext, bool> containsExpected)
        {
            var ctx = SetupMapEnumeratorTest();
            Assert.That(containsExpected(ctx), Is.True);
        }

        [TestCaseSource(nameof(EmptyAnchoredEnumeratorCases))]
        public void AnchoredEnumeratorsHandleEmptyResults(Func<MapEnumeratorTestContext, bool> isEmpty)
        {
            var ctx = SetupMapEnumeratorTest();
            Assert.That(isEmpty(ctx), Is.True);
        }

        private static MapEnumeratorTestContext SetupMapEnumeratorTest()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapSystem = entMan.System<SharedMapSystem>();
            var transformSystem = entMan.System<SharedTransformSystem>();
            var mapId = sim.CreateMap().MapId;
            var grid = mapSystem.CreateGridEntity(mapId);
            var tile = new Vector2i(0, 0);
            var emptyTile = new Vector2i(10, 10);

            mapSystem.SetTile(grid, tile, new Tile(1));
            var anchored = entMan.SpawnEntity(null, mapSystem.GridTileToLocal(grid.Owner, grid.Comp, tile));
            Assert.That(transformSystem.AnchorEntity(anchored), Is.True);

            return new MapEnumeratorTestContext(sim, mapSystem, grid, mapId, anchored, tile, emptyTile);
        }

        private static IEnumerable<TestCaseData> MapEnumeratorCases()
        {
            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAllMapIds().Contains(ctx.MapId)))
                .SetName($"{nameof(MapEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAllMapIds)})");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAllGrids(ctx.MapId).Select(x => x.Owner).Contains(ctx.Grid.Owner)))
                .SetName($"{nameof(MapEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAllGrids)} owner)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAllGrids(ctx.MapId).Select(x => x.Comp).Contains(ctx.Grid.Comp)))
                .SetName($"{nameof(MapEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAllGrids)} comp)");
        }

        private static IEnumerable<TestCaseData> TileEnumeratorCases()
        {
            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAllTiles(ctx.Grid.Owner, ctx.Grid.Comp).Select(x => x.GridIndices).Contains(ctx.Tile)))
                .SetName($"{nameof(TileEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAllTiles)})");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetLocalTilesIntersecting(ctx.Grid.Owner, ctx.Grid.Comp, new Box2(-1, -1, 2, 2)).Select(x => x.GridIndices).Contains(ctx.Tile)))
                .SetName($"{nameof(TileEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetLocalTilesIntersecting)} Box2)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetLocalTilesIntersecting(ctx.Grid.Owner, ctx.Grid.Comp, new Box2Rotated(new Box2(-1, -1, 2, 2), Angle.Zero, Vector2.Zero)).Select(x => x.GridIndices).Contains(ctx.Tile)))
                .SetName($"{nameof(TileEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetLocalTilesIntersecting)} Box2Rotated)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetTilesIntersecting(ctx.Grid.Owner, ctx.Grid.Comp, new Box2(-1, -1, 2, 2)).Select(x => x.GridIndices).Contains(ctx.Tile)))
                .SetName($"{nameof(TileEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetTilesIntersecting)} Box2)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetTilesIntersecting(ctx.Grid.Owner, ctx.Grid.Comp, new Box2Rotated(new Box2(-1, -1, 2, 2), Angle.Zero, Vector2.Zero)).Select(x => x.GridIndices).Contains(ctx.Tile)))
                .SetName($"{nameof(TileEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetTilesIntersecting)} Box2Rotated)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetLocalTilesIntersecting(ctx.Grid.Owner, ctx.Grid.Comp, new Circle(Vector2.Zero, 2)).Select(x => x.GridIndices).Contains(ctx.Tile)))
                .SetName($"{nameof(TileEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetLocalTilesIntersecting)} Circle)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetTilesIntersecting(ctx.Grid.Owner, ctx.Grid.Comp, new Circle(Vector2.Zero, 2)).Select(x => x.GridIndices).Contains(ctx.Tile)))
                .SetName($"{nameof(TileEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetTilesIntersecting)} Circle)");
        }

        private static IEnumerable<TestCaseData> AnchoredEnumeratorCases()
        {
            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAnchoredEntities(ctx.Grid, ctx.Tile).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAnchoredEntities)} Entity<Vector2i>)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAnchoredEntities(ctx.Grid.Owner, ctx.Grid.Comp, ctx.Tile).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAnchoredEntities)} Vector2i)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAnchoredEntities(ctx.Grid, ctx.MapSystem.GridTileToLocal(ctx.Grid.Owner, ctx.Grid.Comp, ctx.Tile)).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAnchoredEntities)} EntityCoordinates entity)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAnchoredEntities(ctx.Grid.Owner, ctx.Grid.Comp, ctx.MapSystem.GridTileToLocal(ctx.Grid.Owner, ctx.Grid.Comp, ctx.Tile)).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAnchoredEntities)} EntityCoordinates)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAnchoredEntities(ctx.Grid, ctx.MapSystem.GridTileToWorld(ctx.Grid.Owner, ctx.Grid.Comp, ctx.Tile)).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAnchoredEntities)} MapCoordinates entity)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAnchoredEntities(ctx.Grid.Owner, ctx.Grid.Comp, ctx.MapSystem.GridTileToWorld(ctx.Grid.Owner, ctx.Grid.Comp, ctx.Tile)).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAnchoredEntities)} MapCoordinates)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetLocalAnchoredEntities(ctx.Grid.Owner, ctx.Grid.Comp, new Box2(-1, -1, 2, 2)).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetLocalAnchoredEntities)} Box2)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAnchoredEntities(ctx.Grid.Owner, ctx.Grid.Comp, new Box2(-1, -1, 2, 2)).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAnchoredEntities)} Box2)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetAnchoredEntities(ctx.Grid.Owner, ctx.Grid.Comp, new Box2Rotated(new Box2(-1, -1, 2, 2), Angle.Zero, Vector2.Zero)).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetAnchoredEntities)} Box2Rotated)");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetLocal(ctx.Grid.Owner, ctx.Grid.Comp, ctx.MapSystem.GridTileToLocal(ctx.Grid.Owner, ctx.Grid.Comp, ctx.Tile)).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetLocal)})");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetInDir(ctx.Grid.Owner, ctx.Grid.Comp, ctx.MapSystem.GridTileToLocal(ctx.Grid.Owner, ctx.Grid.Comp, new Vector2i(0, -1)), Direction.North).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetInDir)})");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetOffset(ctx.Grid.Owner, ctx.Grid.Comp, ctx.MapSystem.GridTileToLocal(ctx.Grid.Owner, ctx.Grid.Comp, new Vector2i(-1, 0)), new Vector2i(1, 0)).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetOffset)})");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetCardinalNeighborCells(ctx.Grid.Owner, ctx.Grid.Comp, ctx.MapSystem.GridTileToLocal(ctx.Grid.Owner, ctx.Grid.Comp, ctx.Tile)).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetCardinalNeighborCells)})");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    ctx.MapSystem.GetCellsInSquareArea(ctx.Grid.Owner, ctx.Grid.Comp, ctx.MapSystem.GridTileToLocal(ctx.Grid.Owner, ctx.Grid.Comp, ctx.Tile), 1).Contains(ctx.Anchored)))
                .SetName($"{nameof(AnchoredEnumeratorsHandleNonEmptyResults)}({nameof(SharedMapSystem.GetCellsInSquareArea)})");
        }

        private static IEnumerable<TestCaseData> EmptyAnchoredEnumeratorCases()
        {
            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    !ctx.MapSystem.GetAnchoredEntities(ctx.Grid, ctx.EmptyTile).Any()))
                .SetName($"{nameof(AnchoredEnumeratorsHandleEmptyResults)}({nameof(SharedMapSystem.GetAnchoredEntities)})");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    !ctx.MapSystem.GetCardinalNeighborCells(ctx.Grid.Owner, ctx.Grid.Comp, ctx.MapSystem.GridTileToLocal(ctx.Grid.Owner, ctx.Grid.Comp, ctx.EmptyTile)).Any()))
                .SetName($"{nameof(AnchoredEnumeratorsHandleEmptyResults)}({nameof(SharedMapSystem.GetCardinalNeighborCells)})");

            yield return new TestCaseData(new Func<MapEnumeratorTestContext, bool>(ctx =>
                    !ctx.MapSystem.GetCellsInSquareArea(ctx.Grid.Owner, ctx.Grid.Comp, ctx.MapSystem.GridTileToLocal(ctx.Grid.Owner, ctx.Grid.Comp, ctx.EmptyTile), 1).Any()))
                .SetName($"{nameof(AnchoredEnumeratorsHandleEmptyResults)}({nameof(SharedMapSystem.GetCellsInSquareArea)})");
        }

        public sealed record MapEnumeratorTestContext(
            ISimulation Simulation,
            SharedMapSystem MapSystem,
            Entity<MapGridComponent> Grid,
            MapId MapId,
            EntityUid Anchored,
            Vector2i Tile,
            Vector2i EmptyTile);

        /// <summary>
        ///     Verifies that the world Bounds of the grid properly expand when tiles are placed.
        /// </summary>
        [Test]
        public void BoundsExpansion()
        {
            var sim = SimulationFactory();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var transformSystem = sim.Resolve<IEntityManager>().System<SharedTransformSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapSystem.CreateGridEntity(mapId, gridOptions);
            transformSystem.SetWorldPosition(grid, new Vector2(3, 5));

            mapSystem.SetTile(grid, new Vector2i(-1, -2), new Tile(1));
            mapSystem.SetTile(grid, new Vector2i(1, 2), new Tile(1));

            var bounds = transformSystem.GetWorldMatrix(grid).TransformBox(grid.Comp.LocalAABB);

            // this is world, so add the grid world pos
            Assert.That(bounds.Bottom, Is.EqualTo(-2+5));
            Assert.That(bounds.Left, Is.EqualTo(-1+3));
            Assert.That(bounds.Top, Is.EqualTo(3+5));
            Assert.That(bounds.Right, Is.EqualTo(2+3));
        }

        /// <summary>
        ///     Verifies that the world bounds of the grid properly contract when a tile is removed.
        /// </summary>
        [Test]
        public void BoundsContract()
        {
            var sim = SimulationFactory();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var transformSystem = sim.Resolve<IEntityManager>().System<SharedTransformSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapSystem.CreateGridEntity(mapId, gridOptions);

            transformSystem.SetWorldPosition(grid, new Vector2(3, 5));

            mapSystem.SetTile(grid, new Vector2i(-1, -2), new Tile(1));
            mapSystem.SetTile(grid, new Vector2i(1, 2), new Tile(1));

            mapSystem.SetTile(grid, new Vector2i(1, 2), Tile.Empty);

            var bounds = transformSystem.GetWorldMatrix(grid).TransformBox(grid.Comp.LocalAABB);

            // this is world, so add the grid world pos
            Assert.That(bounds.Bottom, Is.EqualTo(-2+5));
            Assert.That(bounds.Left, Is.EqualTo(-1+3));
            Assert.That(bounds.Top, Is.EqualTo(-1+5));
            Assert.That(bounds.Right, Is.EqualTo(0+3));
        }

        [Test]
        public void GridTileToChunkIndices()
        {
            var sim = SimulationFactory();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapSystem.CreateGridEntity(mapId, gridOptions);

            var result = mapSystem.GridTileToChunkIndices(grid.Comp, new Vector2i(-9, -1));

            Assert.That(result, Is.EqualTo(new Vector2i(-2, -1)));
        }

        /// <summary>
        ///     Verifies that the local position is centered on the tile, instead of bottom left.
        /// </summary>
        [Test]
        public void ToLocalCentered()
        {
            var sim = SimulationFactory();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapSystem.CreateGridEntity(mapId, gridOptions);

            var result = mapSystem.GridTileToLocal(grid.Owner, grid.Comp, new Vector2i(0, 0)).Position;

            Assert.That(result.X, Is.EqualTo(0.5f));
            Assert.That(result.Y, Is.EqualTo(0.5f));
        }

        [Test]
        public void TryGetTileRefNoTile()
        {
            var sim = SimulationFactory();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapSystem.CreateGridEntity(mapId, gridOptions);

            var foundTile = mapSystem.TryGetTileRef(grid.Owner, grid.Comp, new Vector2i(-9, -1), out var tileRef)
;
            Assert.That(foundTile, Is.False);
            Assert.That(tileRef, Is.EqualTo(new TileRef()));
            Assert.That(grid.Comp.ChunkCount, Is.EqualTo(0));
        }

        [Test]
        public void TryGetTileRefTileExists()
        {
            var sim = SimulationFactory();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapSystem.CreateGridEntity(mapId, gridOptions);

            mapSystem.SetTile(grid, new Vector2i(-9, -1), new Tile(typeId: 1, flags: 1, variant: 1));

            var foundTile = mapSystem.TryGetTileRef(grid.Owner, grid.Comp, new Vector2i(-9, -1), out var tileRef);

            Assert.That(foundTile, Is.True);
            Assert.That(grid.Comp.ChunkCount, Is.EqualTo(1));
            Assert.That(mapSystem.GetMapChunks(grid.Owner, grid.Comp).Keys.ToList()[0], Is.EqualTo(new Vector2i(-2, -1)));
            Assert.That(tileRef, Is.EqualTo(new TileRef(grid.Owner, new Vector2i(-9, -1), new Tile(typeId: 1, flags: 1, variant: 1))));
        }

        [Test]
        public void PointCollidesWithGrid()
        {
            var sim = SimulationFactory();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapSystem.CreateGridEntity(mapId, gridOptions);

            mapSystem.SetTile(grid, new Vector2i(19, 23), new Tile(1));

            var result = mapSystem.CollidesWithGrid(grid.Owner, grid.Comp, new Vector2i(19, 23));

            Assert.That(result, Is.True);
        }

        [Test]
        public void PointNotCollideWithGrid()
        {
            var sim = SimulationFactory();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapSystem.CreateGridEntity(mapId, gridOptions);

            mapSystem.SetTile(grid, new Vector2i(19, 23), new Tile(1));

            var result = mapSystem.CollidesWithGrid(grid.Owner, grid.Comp, new Vector2i(19, 24));

            Assert.That(result, Is.False);
        }
    }
}
