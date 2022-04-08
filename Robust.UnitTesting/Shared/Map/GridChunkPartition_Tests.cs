using System.Linq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, TestOf(typeof(GridChunkPartition))]
    internal sealed class GridChunkPartition_Tests
    {
        /// <summary>
        /// Check the vertices for a single tile being placed.
        /// </summary>
        [Test]
        public void TestTileVertices()
        {
            var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
            var entManager = sim.Resolve<IEntityManager>();
            var mapManager = sim.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var fixtures = entManager.GetComponent<FixturesComponent>(grid.GridEntityId);

            // None
            // Top Left
            grid.SetTile(new Vector2i(0, 0), new Tile(1, TileFlag.None));
            Assert.That(fixtures.FixtureCount, Is.EqualTo(1));

            var poly0 = (PolygonShape) fixtures.Fixtures.ToList()[0].Value.Shape;
            Assert.Multiple(() =>
            {
                Assert.That(poly0.VertexCount, Is.EqualTo(4));
                Assert.That(poly0.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(1f, 0f),
                    new(1f, 1f),
                    new(0f, 1f),
                    new(0f, 0f),
                }));
            });

            // Bottom left
            grid.SetTile(new Vector2i(0, 0), new Tile(1, TileFlag.BottomLeft));
            Assert.That(fixtures.FixtureCount, Is.EqualTo(1));

            var poly1 = (PolygonShape) fixtures.Fixtures.ToList()[0].Value.Shape;
            Assert.Multiple(() =>
            {
                Assert.That(poly1.VertexCount, Is.EqualTo(3));
                Assert.That(poly1.Vertices, Is.EqualTo(new Vector2[]
                {
                new(0f, 0f),
                new(1f, 0f),
                new(0f, 1f)
                }));
            });

            // Bottom Right
            grid.SetTile(new Vector2i(0, 0), new Tile(1, TileFlag.BottomRight));
            Assert.That(fixtures.FixtureCount, Is.EqualTo(1));

            var poly2 = (PolygonShape) fixtures.Fixtures.ToList()[0].Value.Shape;
            Assert.Multiple(() =>
            {
                Assert.That(poly2.VertexCount, Is.EqualTo(3));
                Assert.That(poly2.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(0f, 0f),
                    new(1f, 0f),
                    new(1f, 1f)
                }));
            });

            // Top Right
            grid.SetTile(new Vector2i(0, 0), new Tile(1, TileFlag.TopRight));
            Assert.That(fixtures.FixtureCount, Is.EqualTo(1));

            var poly3 = (PolygonShape) fixtures.Fixtures.ToList()[0].Value.Shape;
            Assert.Multiple(() =>
            {
                Assert.That(poly3.VertexCount, Is.EqualTo(3));
                Assert.That(poly3.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(1f, 0f),
                    new(1f, 1f),
                    new(0f, 1f)
                }));
            });

            // Top Left
            grid.SetTile(new Vector2i(0, 0), new Tile(1, TileFlag.TopLeft));
            Assert.That(fixtures.FixtureCount, Is.EqualTo(1));

            var poly4 = (PolygonShape) fixtures.Fixtures.ToList()[0].Value.Shape;
            Assert.Multiple(() =>
            {
                Assert.That(poly4.VertexCount, Is.EqualTo(3));
                Assert.That(poly4.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(0f, 0f),
                    new(1f, 1f),
                    new(0f, 1f)
                }));
            });

            mapManager.DeleteGrid(grid.Index);
            mapManager.DeleteMap(mapId);
        }

        /// <summary>
        /// If we make each diagonal separately then it should be its own fixture.
        /// </summary>
        [Test]
        public void TestStandalonePartition()
        {
            var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
            var entManager = sim.Resolve<IEntityManager>();
            var mapManager = sim.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var fixtures = entManager.GetComponent<FixturesComponent>(grid.GridEntityId);

            foreach (var dir in new []{TileFlag.BottomLeft, TileFlag.BottomRight, TileFlag.TopLeft, TileFlag.TopRight})
            {
                grid.SetTile(new Vector2i(0, 0), new Tile(1, dir));
                grid.SetTile(new Vector2i(1, 0), new Tile(1, dir));

                Assert.That(fixtures.FixtureCount, Is.EqualTo(2));
            }

            mapManager.DeleteGrid(grid.Index);
            mapManager.DeleteMap(mapId);
        }

        /// <summary>
        /// Checks whether long strings of tiles have the correct vertices applied.
        /// </summary>
        [Test]
        public void TestShapes()
        {
            Assert.That(false);
        }

        // TODO: Convex test for convex and clearly not convex shapes.
        [Test]
        public void TestConvex()
        {
            Assert.That(false);
        }

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
            var chunk = new MapChunk(0, 0, size);

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
