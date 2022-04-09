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
    [TestFixture, TestOf(typeof(SharedGridFixtureSystem))]
    internal sealed class GridChunkPartition_Test
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
        public void TestLineShapes()
        {
            var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
            var entManager = sim.Resolve<IEntityManager>();
            var mapManager = sim.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var fixtures = entManager.GetComponent<FixturesComponent>(grid.GridEntityId);

            void Setup(IMapGrid grid, TileFlag left, TileFlag right)
            {
                grid.SetTile(new Vector2i(0, 0), new Tile(1, left));

                for (var i = 1; i < 4; i++)
                {
                    grid.SetTile(new Vector2i(i, 0), new Tile(1));
                }

                grid.SetTile(new Vector2i(4, 0), new Tile(1, right));
            }

            TileFlag leftSide;
            TileFlag rightSide;
            PolygonShape shape;

            // Didn't do a loop or test cases as it's just easier this way to assert the verts
            {
                leftSide = TileFlag.BottomRight;
                rightSide = TileFlag.BottomLeft;
                Setup(grid, leftSide, rightSide);

                Assert.That(fixtures.FixtureCount, Is.EqualTo(1));
                shape = (PolygonShape)fixtures.Fixtures.ToList()[0].Value.Shape;
                Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(5f, 0f),
                    new(4f, 1f),
                    new(1f, 1f),
                    new(0f, 0f),
                }));

                leftSide = TileFlag.BottomRight;
                rightSide = TileFlag.TopLeft;
                Setup(grid, leftSide, rightSide);

                Assert.That(fixtures.FixtureCount, Is.EqualTo(1));
                shape = (PolygonShape)fixtures.Fixtures.ToList()[0].Value.Shape;
                Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(5f, 1f),
                    new(1f, 1f),
                    new(0f, 0f),
                    new(4f, 0f),
                }));

                leftSide = TileFlag.BottomRight;
                rightSide = TileFlag.None;
                Setup(grid, leftSide, rightSide);

                Assert.That(fixtures.FixtureCount, Is.EqualTo(1));
                shape = (PolygonShape)fixtures.Fixtures.ToList()[0].Value.Shape;
                Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(5f, 0f),
                    new(5f, 1f),
                    new(1f, 1f),
                    new(0f, 0f),
                }));
            }
            {
                leftSide = TileFlag.TopRight;
                rightSide = TileFlag.BottomLeft;
                Setup(grid, leftSide, rightSide);

                Assert.That(fixtures.FixtureCount, Is.EqualTo(1));
                shape = (PolygonShape)fixtures.Fixtures.ToList()[0].Value.Shape;
                Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(5f, 0f),
                    new(4f, 1f),
                    new(0f, 1f),
                    new(1f, 0f),
                }));

                leftSide = TileFlag.TopRight;
                rightSide = TileFlag.TopLeft;
                Setup(grid, leftSide, rightSide);

                Assert.That(fixtures.FixtureCount, Is.EqualTo(1));
                shape = (PolygonShape)fixtures.Fixtures.ToList()[0].Value.Shape;
                Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(5f, 1f),
                    new(0f, 1f),
                    new(1f, 0f),
                    new(4f, 0f),
                }));

                leftSide = TileFlag.TopRight;
                rightSide = TileFlag.None;
                Setup(grid, leftSide, rightSide);

                Assert.That(fixtures.FixtureCount, Is.EqualTo(1));
                shape = (PolygonShape)fixtures.Fixtures.ToList()[0].Value.Shape;
                Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(5f, 0f),
                    new(5f, 1f),
                    new(0f, 1f),
                    new(1f, 0f),
                }));
            }
            {
                leftSide = TileFlag.None;
                rightSide = TileFlag.BottomLeft;
                Setup(grid, leftSide, rightSide);

                Assert.That(fixtures.FixtureCount, Is.EqualTo(1));
                shape = (PolygonShape)fixtures.Fixtures.ToList()[0].Value.Shape;
                Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(5f, 0f),
                    new(4f, 1f),
                    new(0f, 1f),
                    new(0f, 0f),
                }));

                leftSide = TileFlag.None;
                rightSide = TileFlag.TopLeft;
                Setup(grid, leftSide, rightSide);

                Assert.That(fixtures.FixtureCount, Is.EqualTo(1));
                shape = (PolygonShape)fixtures.Fixtures.ToList()[0].Value.Shape;
                Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(5f, 1f),
                    new(0f, 1f),
                    new(0f, 0f),
                    new(4f, 0f),
                }));

                leftSide = TileFlag.None;
                rightSide = TileFlag.None;
                Setup(grid, leftSide, rightSide);

                Assert.That(fixtures.FixtureCount, Is.EqualTo(1));
                shape = (PolygonShape)fixtures.Fixtures.ToList()[0].Value.Shape;
                Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
                {
                    new(5f, 0f),
                    new(5f, 1f),
                    new(0f, 1f),
                    new(0f, 0f),
                }));
            }

            mapManager.DeleteGrid(grid.Index);
            mapManager.DeleteMap(mapId);
        }

        [Test]
        public void TestSquaresShapes()
        {
            var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
            var entManager = sim.Resolve<IEntityManager>();
            var mapManager = sim.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var fixtures = entManager.GetComponent<FixturesComponent>(grid.GridEntityId);
            PolygonShape shape;

            // 4 corners
            grid.SetTile(new Vector2i(0, 0),new Tile(1, TileFlag.TopRight));
            grid.SetTile(new Vector2i(1, 0), new Tile(1, TileFlag.TopLeft));
            grid.SetTile(new Vector2i(1, 1), new Tile(1, TileFlag.BottomLeft));
            grid.SetTile(new Vector2i(0, 1), new Tile(1, TileFlag.BottomRight));

            Assert.That(fixtures.FixtureCount, Is.EqualTo(1));
            shape = (PolygonShape)fixtures.Fixtures.ToList()[0].Value.Shape;
            Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
            {
                new(2f, 1f),
                new(1f, 2f),
                new(0f, 1f),
                new(1f, 0f),
            }));

            // Somehow "convex" shape I found when devving
            grid.SetTile(new Vector2i(0, 0),new Tile(1, TileFlag.BottomRight));
            grid.SetTile(new Vector2i(1, 0), new Tile(1, TileFlag.None));
            grid.SetTile(new Vector2i(1, 1), new Tile(1, TileFlag.BottomLeft));

            Assert.That(fixtures.FixtureCount, Is.EqualTo(2));

            // Add whatever else I find here

            mapManager.DeleteGrid(grid.Index);
            mapManager.DeleteMap(mapId);
        }

        // TODO: Convex test for convex and clearly not convex shapes.
        [Test]
        public void TestConvex()
        {
            Assert.That(true);
        }
    }
}
