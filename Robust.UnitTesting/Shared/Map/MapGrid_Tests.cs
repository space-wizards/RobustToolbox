using System.Linq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;
// ReSharper disable AccessToStaticMemberViaDerivedType

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
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var gridEnt = mapMan.EntityManager.SpawnEntity(null, mapId);
            var gridComp = mapMan.EntityManager.AddComponent<MapGridComponent>(gridEnt);
            gridComp.ChunkSize = 8;

            gridComp.SetTile(new Vector2i(-9, -1), new Tile(1, (TileRenderFlag)1, 1));

            var result = gridComp.GetTileRef(new Vector2i(-9, -1));

            Assert.That(gridComp.ChunkCount, Is.EqualTo(1));
            Assert.That(gridComp.GetMapChunks().Keys.ToList()[0], Is.EqualTo(new Vector2i(-2, -1)));
            Assert.That(result, Is.EqualTo(new TileRef(gridComp.Owner, new Vector2i(-9,-1), new Tile(1, (TileRenderFlag)1, 1))));
        }

        /// <summary>
        ///     Verifies that the world Bounds of the grid properly expand when tiles are placed.
        /// </summary>
        [Test]
        public void BoundsExpansion()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var entMan = sim.Resolve<IEntityManager>();
            var mapId = mapMan.CreateMap();
            var gridEnt = mapMan.EntityManager.SpawnEntity(null, mapId);
            var gridComp = mapMan.EntityManager.AddComponent<MapGridComponent>(gridEnt);
            gridComp.ChunkSize = 8;
            Vector2 val = new Vector2(3, 5);
            var xform = entMan.GetComponent<TransformComponent>(gridComp.Owner);
            xform.WorldPosition = val;

            gridComp.SetTile(new Vector2i(-1, -2), new Tile(1));
            gridComp.SetTile(new Vector2i(1, 2), new Tile(1));

            var bounds = TransformComponent.CalcWorldAabb(xform, gridComp.LocalAABB);

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
            var entMan = sim.Resolve<IEntityManager>();
            var mapId = mapMan.CreateMap();
            var gridEnt = mapMan.EntityManager.SpawnEntity(null, mapId);
            var gridComp = mapMan.EntityManager.AddComponent<MapGridComponent>(gridEnt);
            gridComp.ChunkSize = 8;
            Vector2 val = new Vector2(3, 5);
            entMan.GetComponent<TransformComponent>(gridComp.Owner).WorldPosition = val;

            gridComp.SetTile(new Vector2i(-1, -2), new Tile(1));
            gridComp.SetTile(new Vector2i(1, 2), new Tile(1));

            gridComp.SetTile(new Vector2i(1, 2), Tile.Empty);

            var bounds = TransformComponent.CalcWorldAabb(entMan.GetComponent<TransformComponent>(gridComp.Owner), gridComp.LocalAABB);

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
            var gridEnt = mapMan.EntityManager.SpawnEntity(null, mapId);
            var gridComp = mapMan.EntityManager.AddComponent<MapGridComponent>(gridEnt);
            gridComp.ChunkSize = 8;

            var result = gridComp.GridTileToChunkIndices(new Vector2i(-9, -1));

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
            var gridEnt = mapMan.EntityManager.SpawnEntity(null, mapId);
            var gridComp = mapMan.EntityManager.AddComponent<MapGridComponent>(gridEnt);
            gridComp.ChunkSize = 8;

            var result = gridComp.GridTileToLocal(new Vector2i(0, 0)).Position;

            Assert.That(result.X, Is.EqualTo(0.5f));
            Assert.That(result.Y, Is.EqualTo(0.5f));
        }

        [Test]
        public void TryGetTileRefNoTile()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var gridEnt = mapMan.EntityManager.SpawnEntity(null, mapId);
            var gridComp = mapMan.EntityManager.AddComponent<MapGridComponent>(gridEnt);
            gridComp.ChunkSize = 8;

            var foundTile = gridComp.TryGetTileRef(new Vector2i(-9, -1), out var tileRef);

            Assert.That(foundTile, Is.False);
            Assert.That(tileRef, Is.EqualTo(new TileRef()));
            Assert.That(gridComp.ChunkCount, Is.EqualTo(0));
        }

        [Test]
        public void TryGetTileRefTileExists()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var gridEnt = mapMan.EntityManager.SpawnEntity(null, mapId);
            var gridComp = mapMan.EntityManager.AddComponent<MapGridComponent>(gridEnt);
            gridComp.ChunkSize = 8;

            gridComp.SetTile(new Vector2i(-9, -1), new Tile(1, (TileRenderFlag)1, 1));

            var foundTile = gridComp.TryGetTileRef(new Vector2i(-9, -1), out var tileRef);

            Assert.That(foundTile, Is.True);
            Assert.That(gridComp.ChunkCount, Is.EqualTo(1));
            Assert.That(gridComp.GetMapChunks().Keys.ToList()[0], Is.EqualTo(new Vector2i(-2, -1)));
            Assert.That(tileRef, Is.EqualTo(new TileRef(gridComp.Owner, new Vector2i(-9, -1), new Tile(1, (TileRenderFlag)1, 1))));
        }

        [Test]
        public void PointCollidesWithGrid()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapId = mapMan.CreateMap();
            var gridEnt = mapMan.EntityManager.SpawnEntity(null, mapId);
            var gridComp = mapMan.EntityManager.AddComponent<MapGridComponent>(gridEnt);
            gridComp.ChunkSize = 8;
            var grid = gridComp;

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
            var gridEnt = mapMan.EntityManager.SpawnEntity(null, mapId);
            var gridComp = mapMan.EntityManager.AddComponent<MapGridComponent>(gridEnt);
            gridComp.ChunkSize = 8;
            var grid = gridComp;

            grid.SetTile(new Vector2i(19, 23), new Tile(1));

            var result = grid.CollidesWithGrid(new Vector2i(19, 24));

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// To create a grid, simply add it to any entity. You can modify any fields after it is added.
        /// </summary>
        [Test]
        public void ExampleCreateGrid()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var entMan = sim.Resolve<IEntityManager>();
            var mapId = mapMan.CreateMap();

            var gridEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var gridComp = entMan.AddComponent<MapGridComponent>(gridEnt);

            gridComp.ChunkSize = 5;
            //gridComp.TileSize = 3; //Broken

            gridComp.SetTile(new Vector2i(0, 0), new Tile(1));
            gridComp.SetTile(new Vector2i(5, 0), new Tile(1));

            Assert.That(gridComp.GridIndex, Is.Not.EqualTo(GridId.Invalid));
            Assert.That(gridComp.ChunkSize, Is.EqualTo(5));
            Assert.That(gridComp.TileSize, Is.EqualTo(1));
            Assert.That(gridComp.ChunkCount, Is.EqualTo(2));
            Assert.That(gridComp.LocalAABB, Is.EqualTo(new Box2(0, 0, 6, 1)));
        }
    }
}
