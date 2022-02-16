using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, Parallelizable, TestOf(typeof(GridChunkPartition))]
    internal sealed class GridChunkPartition_Tests : RobustUnitTest
    {
        // origin is top left
        private static readonly int[] _testMiscTiles = {
            1, 1, 0, 0,
            0, 1, 1, 0,
            0, 1, 1, 1,
            1, 1, 0, 0,
        };

        [Test]
        public void PartitionChunk_MiscTiles()
        {
            // Arrange
            var chunk = ChunkFactory(4, _testMiscTiles);

            // Act
            GridChunkPartition.PartitionChunk(chunk, out var bounds, out _);

            // box origin is top left
            // algorithm goes down columns of array, starting on left side, then moves right, expanding rectangles to the right
            /*
            0 2 . .
            . 2 3 .
            . 2 3 4
            1 2 . .
            */

            Assert.That(bounds, Is.EqualTo(new Box2i(0,0,4,4)));
        }

        // origin is top left
        private static readonly int[] _testJoinTiles = {
            0, 1, 1, 0,
            0, 1, 1, 0,
            0, 1, 1, 0,
            0, 0, 0, 0,
        };

        [Test]
        public void PartitionChunk_JoinTiles()
        {
            // Arrange
            var chunk = ChunkFactory(4, _testJoinTiles);

            // Act
            GridChunkPartition.PartitionChunk(chunk, out var bounds, out _);

            Assert.That(bounds, Is.EqualTo(new Box2i(1, 0, 3, 3)));
        }

        private static MapChunk ChunkFactory(ushort size, int[] tiles)
        {
            var fakeGrid = new Mock<IMapGridInternal>();

            var chunk = new MapChunk(fakeGrid.Object, 0, 0, size);

            for (var i = 0; i < tiles.Length; i++)
            {
                var x = i % size;
                var y = i / size;

                chunk.SetTile((ushort)x, (ushort)y, new Tile((ushort)tiles[i]));
            }

            return chunk;
        }
    }
}
