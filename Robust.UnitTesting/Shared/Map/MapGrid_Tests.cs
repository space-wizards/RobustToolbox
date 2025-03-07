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
            var mapMan = sim.Resolve<IMapManager>();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapMan.CreateGridEntity(mapId, gridOptions);

            mapSystem.SetTile(grid, new Vector2i(-9, -1), new Tile(typeId: 1, flags: 1, variant: 1));
            var result = mapSystem.GetTileRef(grid.Owner, grid.Comp, new Vector2i(-9, -1));

            Assert.That(grid.Comp.ChunkCount, Is.EqualTo(1));
            Assert.That(mapSystem.GetMapChunks(grid.Owner, grid.Comp).Keys.ToList()[0], Is.EqualTo(new Vector2i(-2, -1)));
            Assert.That(result, Is.EqualTo(new TileRef(grid.Owner, new Vector2i(-9,-1), new Tile(typeId: 1, flags: 1, variant: 1))));
        }

        /// <summary>
        ///     Verifies that the world Bounds of the grid properly expand when tiles are placed.
        /// </summary>
        [Test]
        public void BoundsExpansion()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var transformSystem = sim.Resolve<IEntityManager>().System<SharedTransformSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapMan.CreateGridEntity(mapId, gridOptions);
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
            var mapMan = sim.Resolve<IMapManager>();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var transformSystem = sim.Resolve<IEntityManager>().System<SharedTransformSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapMan.CreateGridEntity(mapId, gridOptions);

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
            var mapMan = sim.Resolve<IMapManager>();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapMan.CreateGridEntity(mapId, gridOptions);

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
            var mapMan = sim.Resolve<IMapManager>();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapMan.CreateGridEntity(mapId, gridOptions);

            var result = mapSystem.GridTileToLocal(grid.Owner, grid.Comp, new Vector2i(0, 0)).Position;

            Assert.That(result.X, Is.EqualTo(0.5f));
            Assert.That(result.Y, Is.EqualTo(0.5f));
        }

        [Test]
        public void TryGetTileRefNoTile()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapMan.CreateGridEntity(mapId, gridOptions);

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
            var mapMan = sim.Resolve<IMapManager>();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapMan.CreateGridEntity(mapId, gridOptions);

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
            var mapMan = sim.Resolve<IMapManager>();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapMan.CreateGridEntity(mapId, gridOptions);

            mapSystem.SetTile(grid, new Vector2i(19, 23), new Tile(1));

            var result = mapSystem.CollidesWithGrid(grid.Owner, grid.Comp, new Vector2i(19, 23));

            Assert.That(result, Is.True);
        }

        [Test]
        public void PointNotCollideWithGrid()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
            var mapId = sim.CreateMap().MapId;
            var gridOptions = new GridCreateOptions();
            gridOptions.ChunkSize = 8;
            var grid = mapMan.CreateGridEntity(mapId, gridOptions);

            mapSystem.SetTile(grid, new Vector2i(19, 23), new Tile(1));

            var result = mapSystem.CollidesWithGrid(grid.Owner, grid.Comp, new Vector2i(19, 24));

            Assert.That(result, Is.False);
        }
    }
}
