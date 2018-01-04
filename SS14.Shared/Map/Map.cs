using SS14.Shared.Interfaces.Map;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Map
{
    public class Map : IMap
    {
        public int Index { get; private set; } = 0;
        private readonly MapManager _mapManager;
        private readonly Dictionary<int, MapGrid> _grids = new Dictionary<int, MapGrid>();

        public Map(MapManager mapManager, int mapID)
        {
            Index = mapID;
            _mapManager = mapManager;
            CreateGrid(MapManager.DEFAULTGRID);
        }

        #region GridAccess

        /// <summary>
        ///     Creates a new empty grid with the given ID and optional chunk size. If a grid already
        ///     exists with the gridID, it is overwritten with the new grid.
        /// </summary>
        /// <param name="gridId">The id of the new grid to create.</param>
        /// <param name="chunkSize">Optional chunk size of the new grid.</param>
        /// <param name="snapSize">Optional size of the snap grid</param>
        /// <returns></returns>
        public IMapGrid CreateGrid(int gridId, ushort chunkSize = 32, float snapSize = 1)
        {
            var newGrid = new MapGrid(_mapManager, gridId, chunkSize, snapSize, Index);
            _grids.Add(gridId, newGrid);
            _mapManager.RaiseOnGridCreated(Index, gridId);
            return newGrid;
        }

        /// <summary>
        ///     Checks if a grid exists with the given ID.
        /// </summary>
        /// <param name="gridId">The ID of the grid to check.</param>
        /// <returns></returns>
        public bool GridExists(int gridId)
        {
            return _grids.ContainsKey(gridId);
        }

        /// <summary>
        ///     Gets the grid associated with the given grid ID. If the grid with the given ID does not exist, return null.
        /// </summary>
        /// <param name="gridId">The id of the grid to get.</param>
        /// <returns></returns>
        public IMapGrid GetGrid(int gridId)
        {
            _grids.TryGetValue(gridId, out var output);
            return output;
        }

        /// <summary>
        ///     Alias of IMapManager.GetGrid(IMapManager.DefaultGridId);
        /// </summary>
        /// <returns></returns>
        public IMapGrid GetDefaultGrid()
        {
            return GetGrid(MapManager.DEFAULTGRID);
        }

        public IEnumerable<IMapGrid> GetAllGrids()
        {
            foreach (var kgrid in _grids)
            {
                yield return kgrid.Value;
            }
        }

        /// <summary>
        ///     Deletes the grid associated with the given grid ID.
        /// </summary>
        /// <param name="gridId">The grid to remove.</param>
        public void RemoveGrid(int gridId)
        {
            if (!_grids.TryGetValue(gridId, out var output))
                return;

            output.Dispose();
            _grids.Remove(gridId);
            _mapManager.RaiseOnGridRemoved(Index, gridId);
        }

        /// <inheritdoc />
        public IMapGrid FindGridAt(LocalCoordinates worldPos)
        {
            var pos = worldPos.ToWorld().Position;
            IMapGrid grid = GetDefaultGrid();
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(pos) && kvGrid.Value.Index != MapManager.DEFAULTGRID)
                    grid = kvGrid.Value;
            return grid;
        }

        /// <inheritdoc />
        public IMapGrid FindGridAt(Vector2 worldPos)
        {
            IMapGrid grid = GetDefaultGrid();
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(worldPos) && kvGrid.Value.Index != MapManager.DEFAULTGRID)
                    grid = kvGrid.Value;
            return grid;
        }

        /// <summary>
        ///     Finds all grids that intersect the rectangle in the world.
        /// </summary>
        /// <param name="worldArea">The are in world coordinates to search.</param>
        /// <returns></returns>
        public IEnumerable<IMapGrid> FindGridsIntersecting(OpenTK.Box2 worldArea)
        {
            var gridList = new List<MapGrid>();
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Intersects(worldArea))
                    gridList.Add(kvGrid.Value);
            return gridList;
        }

        #endregion GridAccess
    }
}
