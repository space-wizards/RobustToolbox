using Moq;
using NUnit.Framework;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Map;
using MapGrid = Robust.Shared.Map.MapManager.MapGrid;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, Parallelizable, TestOf(typeof(MapGrid))]
    class MapGrid_Tests
    {
        /// <summary>
        ///     Verifies that the world Bounds of the grid properly expand when tiles are placed.
        /// </summary>
        [Test]
        public void MapGridBoundsExpansion()
        {
            var grid = MapGridFactory(new GridId(1));

            grid.SetTile(new MapIndices(-1, -2), new Tile(1));
            grid.SetTile(new MapIndices(1, 2), new Tile(1));

            var bounds = grid.WorldBounds;

            Assert.That(bounds.Bottom, Is.EqualTo(-2));
            Assert.That(bounds.Left, Is.EqualTo(-1));
            Assert.That(bounds.Top, Is.EqualTo(3));
            Assert.That(bounds.Right, Is.EqualTo(2));
        }

        /// <summary>
        ///     Verifies that the world bounds of the grid properly contract when a tile is removed.
        /// </summary>
        [Test]
        public void MapGridBoundsContract()
        {
            var grid = MapGridFactory(new GridId(1));

            grid.SetTile(new MapIndices(-1, -2), new Tile(1));
            grid.SetTile(new MapIndices(1, 2), new Tile(1));

            grid.SetTile(new MapIndices(1, 2), Tile.Empty);

            var bounds = grid.WorldBounds;
            Assert.That(bounds.Bottom, Is.EqualTo(-2));
            Assert.That(bounds.Left, Is.EqualTo(-1));
            Assert.That(bounds.Top, Is.EqualTo(-1));
            Assert.That(bounds.Right, Is.EqualTo(0));
        }

        /// <summary>
        ///     Verifies that the local position is centered on the tile, instead of bottom left.
        /// </summary>
        [Test]
        public void MapGridToLocalCentered()
        {
            var grid = MapGridFactory(new GridId(1));

            var result = grid.GridTileToLocal(new MapIndices(0, 0)).Position;

            Assert.That(result.X, Is.EqualTo(0.5f));
            Assert.That(result.Y, Is.EqualTo(0.5f));
        }

        private static MapGrid MapGridFactory(GridId id)
        {
            var timing = new Mock<IGameTiming>();
            var mapMan = new Mock<IMapManagerInternal>();
            mapMan.SetupGet(p => p.GameTiming).Returns(timing.Object);

            return new MapGrid(mapMan.Object, id, 8, 1, new MapId(3));
        }
    }
}
