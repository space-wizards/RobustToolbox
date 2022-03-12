using System.Linq;
using Moq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, TestOf(typeof(MapGrid))]
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
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var grid = (IMapGridInternal)mapMan.CreateGrid(mapId, null, 8);

            grid.SetTile(new Vector2i(-9, -1), new Tile(1, (TileRenderFlag)1, 1));

            var result = grid.GetTileRef(new Vector2i(-9, -1));

            Assert.That(grid.ChunkCount, Is.EqualTo(1));
            Assert.That(grid.GetMapChunks().Keys.ToList()[0], Is.EqualTo(new Vector2i(-2, -1)));
            Assert.That(result, Is.EqualTo(new TileRef(mapId, grid.Index, new Vector2i(-9,-1), new Tile(1, (TileRenderFlag)1, 1))));
        }

        /// <summary>
        ///     Verifies that the world Bounds of the grid properly expand when tiles are placed.
        /// </summary>
        [Test]
        public void BoundsExpansion()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var grid = (IMapGridInternal)mapMan.CreateGrid(mapId, null, 8);
            grid.WorldPosition = new Vector2(3, 5);

            grid.SetTile(new Vector2i(-1, -2), new Tile(1));
            grid.SetTile(new Vector2i(1, 2), new Tile(1));

            var bounds = grid.WorldBounds;

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
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var grid = (IMapGridInternal)mapMan.CreateGrid(mapId, null, 8);
            grid.WorldPosition = new Vector2(3, 5);

            grid.SetTile(new Vector2i(-1, -2), new Tile(1));
            grid.SetTile(new Vector2i(1, 2), new Tile(1));

            grid.SetTile(new Vector2i(1, 2), Tile.Empty);

            var bounds = grid.WorldBounds;

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
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var grid = (IMapGridInternal)mapMan.CreateGrid(mapId, null, 8);

            var result = grid.GridTileToChunkIndices(new Vector2i(-9, -1));

            Assert.That(result, Is.EqualTo(new Vector2i(-2, -1)));
        }

        /// <summary>
        ///     Verifies that the local position is centered on the tile, instead of bottom left.
        /// </summary>
        [Test]
        public void ToLocalCentered()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var grid = (IMapGridInternal)mapMan.CreateGrid(mapId, null, 8);

            var result = grid.GridTileToLocal(new Vector2i(0, 0)).Position;

            Assert.That(result.X, Is.EqualTo(0.5f));
            Assert.That(result.Y, Is.EqualTo(0.5f));
        }

        [Test]
        public void TryGetTileRefNoTile()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var grid = (IMapGridInternal)mapMan.CreateGrid(mapId, null, 8);

            var foundTile = grid.TryGetTileRef(new Vector2i(-9, -1), out var tileRef);

            Assert.That(foundTile, Is.False);
            Assert.That(tileRef, Is.EqualTo(new TileRef()));
            Assert.That(grid.ChunkCount, Is.EqualTo(0));
        }

        [Test]
        public void TryGetTileRefTileExists()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var grid = (IMapGridInternal)mapMan.CreateGrid(mapId, null, 8);

            grid.SetTile(new Vector2i(-9, -1), new Tile(1, (TileRenderFlag)1, 1));

            var foundTile = grid.TryGetTileRef(new Vector2i(-9, -1), out var tileRef);

            Assert.That(foundTile, Is.True);
            Assert.That(grid.ChunkCount, Is.EqualTo(1));
            Assert.That(grid.GetMapChunks().Keys.ToList()[0], Is.EqualTo(new Vector2i(-2, -1)));
            Assert.That(tileRef, Is.EqualTo(new TileRef(mapId, grid.Index, new Vector2i(-9, -1), new Tile(1, (TileRenderFlag)1, 1))));
        }

        [Test]
        public void PointCollidesWithGrid()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var grid = (IMapGridInternal)mapMan.CreateGrid(mapId, null, 8);

            grid.SetTile(new Vector2i(19, 23), new Tile(1));

            var result = grid.CollidesWithGrid(new Vector2i(19, 23));

            Assert.That(result, Is.True);
        }

        [Test]
        public void PointNotCollideWithGrid()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var grid = (IMapGridInternal)mapMan.CreateGrid(mapId, null, 8);

            grid.SetTile(new Vector2i(19, 23), new Tile(1));

            var result = grid.CollidesWithGrid(new Vector2i(19, 24));

            Assert.That(result, Is.False);
        }
    }
}
