using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, Parallelizable, TestOf(typeof(MapChunk))]
    class MapChunk_Tests
    {
        [Test]
        public void GetChunkSize()
        {
            var chunk = MapChunkFactory(7, 9);

            Assert.That(chunk.ChunkSize, Is.EqualTo((ushort)8));
        }

        [Test]
        public void GetIndices()
        {
            var chunk = MapChunkFactory(7, 9);

            Assert.That(chunk.X, Is.EqualTo(7));
            Assert.That(chunk.Y, Is.EqualTo(9));
            Assert.That(chunk.Indices, Is.EqualTo(new Vector2i(7,9)));
        }

        [Test]
        public void ConstructorSetsLastTick()
        {
            var chunk = MapChunkFactory(7, 9);

            Assert.That(chunk.LastModifiedTick, Is.EqualTo(new GameTick(11)));
        }

        [Test]
        public void SetTileThrowsOutOfRange()
        {
            var chunk = MapChunkFactory(7, 9);

            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.SetTile(8,0, new Tile())));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.SetTile(0, 8, new Tile())));
        }

        [Test]
        public void SetTileModifiesLastTick()
        {
            var curTick = new GameTick(11);
            var mapGrid = new Mock<IMapGridInternal>();
            mapGrid.SetupGet(f => f.CurTick).Returns((() => curTick));
            mapGrid.SetupGet(f => f.ParentMapId).Returns(new MapId(11));
            mapGrid.SetupGet(f => f.Index).Returns(new GridId(13));

            var chunk = new MapChunk(mapGrid.Object, 7, 9, 8);

            curTick = new GameTick(13);
            chunk.SetTile(3, 5, new Tile(1, 3));

            Assert.That(chunk.LastModifiedTick, Is.EqualTo(new GameTick(13)));
        }

        [Test]
        public void SetTileDuplicateDoesNotModifyLastTick()
        {
            var curTick = new GameTick(11);
            var mapGrid = new Mock<IMapGridInternal>();
            mapGrid.SetupGet(f => f.CurTick).Returns((() => curTick));
            mapGrid.SetupGet(f => f.ParentMapId).Returns(new MapId(11));
            mapGrid.SetupGet(f => f.Index).Returns(new GridId(13));

            var chunk = new MapChunk(mapGrid.Object, 7, 9, 8);

            curTick = new GameTick(13);
            chunk.SetTile(3, 5, new Tile(1, 3));
            curTick = new GameTick(14);
            chunk.SetTile(3, 5, new Tile(1, 3));

            Assert.That(chunk.LastModifiedTick, Is.EqualTo(new GameTick(13)));
        }

        [Test]
        public void GetTileRefByIndex()
        {
            var chunk = MapChunkFactory(7, 9);
            chunk.SetTile(3, 5, new Tile(1, 3));

            var result = chunk.GetTileRef(3, 5);

            Assert.That(result.X, Is.EqualTo(8 * 7 + 3));
            Assert.That(result.Y, Is.EqualTo(8 * 9 + 5));
            Assert.That(result.Tile.TypeId, Is.EqualTo(1));
            Assert.That(result.Tile.Data, Is.EqualTo((ushort) 3));
            Assert.That(result.GridIndex, Is.EqualTo(new GridId(13)));
            Assert.That(result.MapIndex, Is.EqualTo(new MapId(11)));
        }

        [Test]
        public void GetTileRefByIndices()
        {
            var chunk = MapChunkFactory(7, 9);
            chunk.SetTile(3, 5, new Tile(1, 3));

            var result = chunk.GetTileRef(new Vector2i(3, 5));

            Assert.That(result.X, Is.EqualTo(8 * 7 + 3));
            Assert.That(result.Y, Is.EqualTo(8 * 9 + 5));
            Assert.That(result.Tile.TypeId, Is.EqualTo(1));
            Assert.That(result.Tile.Data, Is.EqualTo((ushort)3));
            Assert.That(result.GridIndex, Is.EqualTo(new GridId(13)));
            Assert.That(result.MapIndex, Is.EqualTo(new MapId(11)));
        }

        [Test]
        public void GetTileRefThrowsOutOfRange()
        {
            var chunk = MapChunkFactory(7, 9);

            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.GetTileRef(8, 0)));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.GetTileRef(0, 8)));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.GetTileRef(new Vector2i(8,0))));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.GetTileRef(new Vector2i(0, 8))));
        }

        [Test]
        public void GetTileByIndex()
        {
            var chunk = MapChunkFactory(7, 9);
            chunk.SetTile(3, 5, new Tile(1, 3));

            var result = chunk.GetTile(3, 5);

            Assert.That(result.TypeId, Is.EqualTo(1));
            Assert.That(result.Data, Is.EqualTo((ushort)3));
        }

        [Test]
        public void GetTileByIndexThrowsOutOfRange()
        {
            var chunk = MapChunkFactory(7, 9);

            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.GetTile(8, 0)));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.GetTile(0, 8)));
        }

        [Test]
        public void GetAllTilesNotEmpty()
        {
            var chunk = MapChunkFactory(7, 9);
            chunk.SetTile(5, 4, new Tile(5, 7));
            chunk.SetTile(3, 5, new Tile(1, 3));

            var tiles = chunk.GetAllTiles().ToList();

            Assert.That(tiles.Count, Is.EqualTo(2));

            // Order is guaranteed to be row-major (because C# is), the Y value is contiguous in memory
            Assert.That(tiles[0],
                Is.EqualTo(new TileRef(new MapId(11),
                    new GridId(13),
                    new Vector2i(8 * 7 + 3, 8 * 9 + 5),
                    new Tile(1, 3))));

            Assert.That(tiles[1],
                Is.EqualTo(new TileRef(new MapId(11),
                    new GridId(13),
                    new Vector2i(8 * 7 + 5, 8 * 9 + 4),
                    new Tile(5, 7))));
        }

        [Test]
        public void GetAllTilesEmpty()
        {
            var chunk = MapChunkFactory(7, 9);
            chunk.SetTile(5, 4, new Tile(5, 7));
            chunk.SetTile(3, 5, new Tile(1, 3));

            var tiles = chunk.GetAllTiles(false).ToList();

            Assert.That(tiles.Count, Is.EqualTo(8*8));

            // Order is guaranteed to be row-major (because C# is), the Y value is contiguous in memory
            Assert.That(tiles[8*3+5],
                Is.EqualTo(new TileRef(new MapId(11),
                    new GridId(13),
                    new Vector2i(8 * 7 + 3, 8 * 9 + 5),
                    new Tile(1, 3))));

            Assert.That(tiles[8*5+4],
                Is.EqualTo(new TileRef(new MapId(11),
                    new GridId(13),
                    new Vector2i(8 * 7 + 5, 8 * 9 + 4),
                    new Tile(5, 7))));
        }

        [Test]
        public void SetTileNotifiesGrid()
        {
            var mapGrid = new Mock<IMapGridInternal>();
            mapGrid.SetupGet(f => f.CurTick).Returns(new GameTick(11));
            mapGrid.SetupGet(f => f.ParentMapId).Returns(new MapId(11));
            mapGrid.SetupGet(f => f.Index).Returns(new GridId(13));
            mapGrid.Setup(f => f.NotifyTileChanged(It.Ref<TileRef>.IsAny, It.Ref<Tile>.IsAny)).Verifiable();

            var chunk = new MapChunk(mapGrid.Object, 7, 9, 8);
            chunk.SetTile(3, 5, new Tile(1, 3));

            mapGrid.Verify(f => f.NotifyTileChanged(It.Ref<TileRef>.IsAny, It.Ref<Tile>.IsAny), Times.Once);
        }

        [Test]
        public void SetTileDuplicateNotifiesOnce()
        {
            var mapGrid = new Mock<IMapGridInternal>();
            mapGrid.SetupGet(f => f.CurTick).Returns(new GameTick(11));
            mapGrid.SetupGet(f => f.ParentMapId).Returns(new MapId(11));
            mapGrid.SetupGet(f => f.Index).Returns(new GridId(13));
            mapGrid.Setup(f => f.NotifyTileChanged(It.Ref<TileRef>.IsAny, It.Ref<Tile>.IsAny)).Verifiable();

            var chunk = new MapChunk(mapGrid.Object, 7, 9, 8);
            chunk.SetTile(3, 5, new Tile(1, 3));
            chunk.SetTile(3, 5, new Tile(1, 3));

            mapGrid.Verify(f => f.NotifyTileChanged(It.Ref<TileRef>.IsAny, It.Ref<Tile>.IsAny), Times.Once);
        }

        [Test]
        public void EnumerateTiles()
        {
            var chunk = MapChunkFactory(7, 9);
            chunk.SetTile(5, 4, new Tile(5, 7));
            chunk.SetTile(3, 5, new Tile(1, 3));

            var tiles = new List<TileRef>();
            foreach (var tileRef in chunk)
            {
                tiles.Add(tileRef);
            }

            Assert.That(tiles.Count, Is.EqualTo(2));

            // Order is guaranteed to be row-major (because C# is), the Y value is contiguous in memory
            Assert.That(tiles[0],
                Is.EqualTo(new TileRef(new MapId(11),
                    new GridId(13),
                    new Vector2i(8 * 7 + 3, 8 * 9 + 5),
                    new Tile(1, 3))));

            Assert.That(tiles[1],
                Is.EqualTo(new TileRef(new MapId(11),
                    new GridId(13),
                    new Vector2i(8 * 7 + 5, 8 * 9 + 4),
                    new Tile(5, 7))));
        }

        [Test]
        public void GridToChunkIndices()
        {
            // chunk indices are relative to the lowest coordinates of the chunk
            // EX 8x8 chunk (0,0) occupies tiles 0 to 8 on each axis
            // 8x8 chunk (-1,-1) occupies tiles -8 to -1 on each axis
            var chunk = MapChunkFactory(-1, -1);

            var indices = chunk.GridTileToChunkTile(new Vector2i(-3, -5));

            // drawing this out helps a ton
            // grid tile -1,-1 is chunk tile 7,7
            // grid tile -8,-8 is chunk tile 0,0
            Assert.That(indices, Is.EqualTo(new Vector2i(5, 3)));
        }

        [Test]
        public void GetToString()
        {
            var chunk = MapChunkFactory(7, 9);

            var result = chunk.ToString();

            Assert.That(result, Is.EqualTo("Chunk (7, 9)"));
        }

        [Test]
        public void GetEmptySnapGrid()
        {
            var chunk = MapChunkFactory(7, 9);

            Assert.That(chunk.GetSnapGridCell(0,0).ToList().Count, Is.EqualTo(0));
            Assert.That(chunk.GetSnapGridCell(0, 0).ToList().Count, Is.EqualTo(0));
        }

        [Test]
        public void GetSnapGridThrowsOutOfRange()
        {
            var chunk = MapChunkFactory(7, 9);

            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.GetSnapGridCell(8,0).ToList()));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.GetSnapGridCell(0, 8).ToList()));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.GetSnapGridCell(8,0).ToList()));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.GetSnapGridCell(0, 8).ToList()));
        }

        [Test]
        public void AddSnapGridCellCenter()
        {
            var chunk = MapChunkFactory(7, 9);

            var snapGridComponent = new SnapGridComponent();
            chunk.AddToSnapGridCell(3, 5, snapGridComponent);
            chunk.AddToSnapGridCell(3, 5, new SnapGridComponent());
            chunk.AddToSnapGridCell(3, 6, new SnapGridComponent());

            var result = chunk.GetSnapGridCell(3, 5).ToList();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(snapGridComponent));
        }

        [Test]
        public void AddSnapGridCellEdge()
        {
            var chunk = MapChunkFactory(7, 9);

            var snapGridComponent = new SnapGridComponent();
            chunk.AddToSnapGridCell(3, 5, snapGridComponent);
            chunk.AddToSnapGridCell(3, 5, new SnapGridComponent());
            chunk.AddToSnapGridCell(3, 6, new SnapGridComponent());

            var result = chunk.GetSnapGridCell(3, 5).ToList();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(snapGridComponent));
        }

        [Test]
        public void AddSnapGridThrowsOutOfRange()
        {
            var chunk = MapChunkFactory(7, 9);

            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.AddToSnapGridCell(8, 0, new SnapGridComponent())));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.AddToSnapGridCell(0, 8, new SnapGridComponent())));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.AddToSnapGridCell(8, 0, new SnapGridComponent())));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.AddToSnapGridCell(0, 8, new SnapGridComponent())));
        }

        [Test]
        public void RemoveSnapGridCellCenter()
        {
            var chunk = MapChunkFactory(7, 9);

            var snapGridComponent = new SnapGridComponent();
            chunk.AddToSnapGridCell(3, 5, snapGridComponent);
            chunk.AddToSnapGridCell(3, 5, new SnapGridComponent());
            chunk.AddToSnapGridCell(3, 6, new SnapGridComponent());

            chunk.RemoveFromSnapGridCell(3, 5, snapGridComponent);

            var result = chunk.GetSnapGridCell(3, 5).ToList();
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveSnapGridCellEdge()
        {
            var chunk = MapChunkFactory(7, 9);

            var snapGridComponent = new SnapGridComponent();
            chunk.AddToSnapGridCell(3, 5, snapGridComponent);
            chunk.AddToSnapGridCell(3, 5, new SnapGridComponent());
            chunk.AddToSnapGridCell(3, 6, new SnapGridComponent());

            chunk.RemoveFromSnapGridCell(3, 5, snapGridComponent);

            var result = chunk.GetSnapGridCell(3, 5).ToList();
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveSnapGridThrowsOutOfRange()
        {
            var chunk = MapChunkFactory(7, 9);

            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.RemoveFromSnapGridCell(8, 0, new SnapGridComponent())));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.RemoveFromSnapGridCell(0, 8, new SnapGridComponent())));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.RemoveFromSnapGridCell(8, 0, new SnapGridComponent())));
            Assert.Throws<ArgumentOutOfRangeException>((() => chunk.RemoveFromSnapGridCell(0, 8, new SnapGridComponent())));
        }

        [Test]
        public void BoundsExpandWhenTileAdded()
        {
            var chunk = MapChunkFactory(7, 9);

            chunk.SetTile(3, 5, new Tile(1));
            chunk.SetTile(5, 7, new Tile(1));

            var bounds = chunk.CalcLocalBounds();

            Assert.That(bounds.Left, Is.EqualTo(3));
            Assert.That(bounds.Bottom, Is.EqualTo(5));
            Assert.That(bounds.Right, Is.EqualTo(6));
            Assert.That(bounds.Top, Is.EqualTo(8));
        }

        [Test]
        public void BoundsContractWhenTileRemovedBL()
        {
            var chunk = MapChunkFactory(7, 9);

            chunk.SetTile(3, 5, new Tile(1));
            chunk.SetTile(5, 7, new Tile(1));

            chunk.SetTile(3, 5, Tile.Empty);

            var bounds = chunk.CalcLocalBounds();

            Assert.That(bounds.Left, Is.EqualTo(5));
            Assert.That(bounds.Bottom, Is.EqualTo(7));
            Assert.That(bounds.Right, Is.EqualTo(6));
            Assert.That(bounds.Top, Is.EqualTo(8));
        }

        [Test]
        public void BoundsContractWhenTileRemovedTR()
        {
            var chunk = MapChunkFactory(7, 9);

            chunk.SetTile(3, 5, new Tile(1));
            chunk.SetTile(5, 7, new Tile(1));

            chunk.SetTile(5, 7, Tile.Empty);

            var bounds = chunk.CalcLocalBounds();

            Assert.That(bounds.Left, Is.EqualTo(3));
            Assert.That(bounds.Bottom, Is.EqualTo(5));
            Assert.That(bounds.Right, Is.EqualTo(4));
            Assert.That(bounds.Top, Is.EqualTo(6));
        }

        [Test]
        public void PointCollideWithChunk()
        {
            var chunk = MapChunkFactory(7, 9);
            chunk.SetTile(3, 5, new Tile(1));

            var result = chunk.CollidesWithChunk(new Vector2i(3, 5));

            Assert.That(result, Is.True);
        }

        [Test]
        public void PointNotCollideWithChunk()
        {
            var chunk = MapChunkFactory(7, 9);
            chunk.SetTile(3, 5, new Tile(1));

            var result = chunk.CollidesWithChunk(new Vector2i(3, 6));

            Assert.That(result, Is.False);
        }

        public IMapChunkInternal MapChunkFactory(int xChunkIndex, int yChunkIndex)
        {
            var mapGrid = new Mock<IMapGridInternal>();
            mapGrid.SetupGet(f=>f.CurTick).Returns(new GameTick(11));
            mapGrid.SetupGet(f => f.ParentMapId).Returns(new MapId(11));
            mapGrid.SetupGet(f => f.Index).Returns(new GridId(13));

            return new MapChunk(mapGrid.Object, xChunkIndex, yChunkIndex, 8);
        }
    }
}
