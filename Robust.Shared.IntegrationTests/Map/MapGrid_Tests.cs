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

        [Test]
        public void MapEnumeratorsHandleEmptyAndNonEmptyResults()
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

            Assert.Multiple(() =>
            {
                Assert.That(mapSystem.GetAllMapIds(), Does.Contain(mapId));
                Assert.That(mapSystem.GetAllGrids(mapId).Select(x => x.Owner), Does.Contain(grid.Owner));
                Assert.That(mapSystem.GetAllGrids(mapId).Select(x => x.Comp), Does.Contain(grid.Comp));

                Assert.That(mapSystem.GetAllTiles(grid.Owner, grid.Comp).Select(x => x.GridIndices), Does.Contain(tile));
                Assert.That(mapSystem.GetLocalTilesIntersecting(grid.Owner, grid.Comp, new Box2(-1, -1, 2, 2)).Select(x => x.GridIndices), Does.Contain(tile));
                Assert.That(mapSystem.GetLocalTilesIntersecting(grid.Owner, grid.Comp, new Box2Rotated(new Box2(-1, -1, 2, 2), Angle.Zero, Vector2.Zero)).Select(x => x.GridIndices), Does.Contain(tile));
                Assert.That(mapSystem.GetTilesIntersecting(grid.Owner, grid.Comp, new Box2(-1, -1, 2, 2)).Select(x => x.GridIndices), Does.Contain(tile));
                Assert.That(mapSystem.GetTilesIntersecting(grid.Owner, grid.Comp, new Box2Rotated(new Box2(-1, -1, 2, 2), Angle.Zero, Vector2.Zero)).Select(x => x.GridIndices), Does.Contain(tile));
                Assert.That(mapSystem.GetLocalTilesIntersecting(grid.Owner, grid.Comp, new Circle(Vector2.Zero, 2)).Select(x => x.GridIndices), Does.Contain(tile));
                Assert.That(mapSystem.GetTilesIntersecting(grid.Owner, grid.Comp, new Circle(Vector2.Zero, 2)).Select(x => x.GridIndices), Does.Contain(tile));

                Assert.That(mapSystem.GetAnchoredEntities(grid, tile), Does.Contain(anchored));
                Assert.That(mapSystem.GetAnchoredEntities(grid.Owner, grid.Comp, tile), Does.Contain(anchored));
                Assert.That(mapSystem.GetAnchoredEntities(grid, mapSystem.GridTileToLocal(grid.Owner, grid.Comp, tile)), Does.Contain(anchored));
                Assert.That(mapSystem.GetAnchoredEntities(grid.Owner, grid.Comp, mapSystem.GridTileToLocal(grid.Owner, grid.Comp, tile)), Does.Contain(anchored));
                Assert.That(mapSystem.GetAnchoredEntities(grid, mapSystem.GridTileToWorld(grid.Owner, grid.Comp, tile)), Does.Contain(anchored));
                Assert.That(mapSystem.GetAnchoredEntities(grid.Owner, grid.Comp, mapSystem.GridTileToWorld(grid.Owner, grid.Comp, tile)), Does.Contain(anchored));
                Assert.That(mapSystem.GetLocalAnchoredEntities(grid.Owner, grid.Comp, new Box2(-1, -1, 2, 2)), Does.Contain(anchored));
                Assert.That(mapSystem.GetAnchoredEntities(grid.Owner, grid.Comp, new Box2(-1, -1, 2, 2)), Does.Contain(anchored));
                Assert.That(mapSystem.GetAnchoredEntities(grid.Owner, grid.Comp, new Box2Rotated(new Box2(-1, -1, 2, 2), Angle.Zero, Vector2.Zero)), Does.Contain(anchored));
                Assert.That(mapSystem.GetLocal(grid.Owner, grid.Comp, mapSystem.GridTileToLocal(grid.Owner, grid.Comp, tile)), Does.Contain(anchored));
                Assert.That(mapSystem.GetInDir(grid.Owner, grid.Comp, mapSystem.GridTileToLocal(grid.Owner, grid.Comp, new Vector2i(0, -1)), Direction.North), Does.Contain(anchored));
                Assert.That(mapSystem.GetOffset(grid.Owner, grid.Comp, mapSystem.GridTileToLocal(grid.Owner, grid.Comp, new Vector2i(-1, 0)), new Vector2i(1, 0)), Does.Contain(anchored));
                Assert.That(mapSystem.GetCardinalNeighborCells(grid.Owner, grid.Comp, mapSystem.GridTileToLocal(grid.Owner, grid.Comp, tile)), Does.Contain(anchored));
                Assert.That(mapSystem.GetCellsInSquareArea(grid.Owner, grid.Comp, mapSystem.GridTileToLocal(grid.Owner, grid.Comp, tile), 1), Does.Contain(anchored));

                Assert.That(mapSystem.GetAnchoredEntities(grid, emptyTile), Is.Empty);
                Assert.That(mapSystem.GetCardinalNeighborCells(grid.Owner, grid.Comp, mapSystem.GridTileToLocal(grid.Owner, grid.Comp, emptyTile)), Is.Empty);
                Assert.That(mapSystem.GetCellsInSquareArea(grid.Owner, grid.Comp, mapSystem.GridTileToLocal(grid.Owner, grid.Comp, emptyTile), 1), Is.Empty);
            });
        }

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
