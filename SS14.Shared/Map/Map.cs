using OpenTK;
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
        public int Index = 0;
        private readonly MapManager _mapManager;
        private readonly Dictionary<int, MapGrid> _grids = new Dictionary<int, MapGrid>();

        public Map(MapManager mapManager, int mapID)
        {
            Index = mapID;
            _mapManager = mapManager;
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
        public IMapGrid CreateGrid(int gridId, ushort chunkSize = 32, float snapSize = 1, int mapID = 0)
        {
            var newGrid = new MapGrid(_mapManager, gridId, chunkSize, snapSize, mapID);
            _grids.Add(gridId, newGrid);
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
            MapGrid output;
            _grids.TryGetValue(gridId, out output);
            return output;
        }

        /// <summary>
        ///     Gets the grid associated with the given grid ID.
        /// </summary>
        /// <param name="gridId">The id of the grid to get.</param>
        /// <param name="mapGrid">The grid associated with the grid ID. If no grid exists, this is null.</param>
        /// <returns></returns>
        public bool TryGetGrid(int gridId, out IMapGrid mapGrid)
        {
            mapGrid = GetGrid(gridId);
            return mapGrid != null;
        }

        /// <summary>
        ///     Alias of IMapManager.GetGrid(IMapManager.DefaultGridId);
        /// </summary>
        /// <returns></returns>
        public IMapGrid GetDefaultGrid()
        {
            return GetGrid(0);
        }

        /// <summary>
        ///     Deletes the grid associated with the given grid ID.
        /// </summary>
        /// <param name="gridId">The grid to remove.</param>
        public void RemoveGrid(int gridId)
        {
            MapGrid output;
            if (!_grids.TryGetValue(gridId, out output))
                return;

            output.Dispose();
            _grids.Remove(gridId);
        }

        /// <summary>
        ///     Is there any grid at this position in the world?
        /// </summary>
        /// <param name="xWorld">The X coordinate in the world.</param>
        /// <param name="yWorld">The Y coordinate in the world.</param>
        /// <returns>True if there is any grid at the location.</returns>
        public bool IsGridAt(LocalCoordinates posWorld)
        {
            var pos = posWorld.Position;
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(pos))
                    return true;
            return false;
        }

        /// <summary>
        ///     Is the specified grid at this position in the world?
        /// </summary>
        /// <param name="xWorld">The X coordinate in the world.</param>
        /// <param name="yWorld">The Y coordinate in the world.</param>
        /// <param name="gridId">The grid id to find.</param>
        /// <returns></returns>
        public bool IsGridAt(LocalCoordinates worldPos, int gridId)
        {
            var pos = worldPos.Position;
            return _grids.TryGetValue(gridId, out MapGrid output) && output.AABBWorld.Contains(pos);
        }

        /// <summary>
        ///     Finds all of the grids at this position in the world.
        /// </summary>
        /// <param name="xWorld">The X coordinate in the world.</param>
        /// <param name="yWorld">The Y coordinate in the world.</param>
        /// <returns></returns>
        public IEnumerable<IMapGrid> FindGridsAt(LocalCoordinates worldPos)
        {
            var pos = worldPos.Position;
            var gridList = new List<MapGrid>();
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(pos))
                    gridList.Add(kvGrid.Value);
            return gridList;
        }

        /// <summary>
        ///     Finds the grid at this world coordinate
        /// </summary>
        /// <param name="xWorld">The X coordinate in the world.</param>
        /// <param name="yWorld">The Y coordinate in the world.</param>
        public bool TryFindGridAt(LocalCoordinates worldPos, out IMapGrid currentgrid)
        {
            var pos = worldPos.Position;
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(pos))
                {
                    currentgrid = kvGrid.Value;
                    return true;
                }
            currentgrid = null;
            return false;
        }

        /// <summary>
        ///     Finds the grid at this world coordinate
        /// </summary>
        /// <param name="WorldPos">The X coordinate in the world.</param>
        public bool TryFindGridAt(Vector2 worldPos, out IMapGrid currentgrid)
        {
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(worldPos))
                {
                    currentgrid = kvGrid.Value;
                    return true;
                }
            currentgrid = GetDefaultGrid();
            return false;
        }

        /// <summary>
        ///     Finds all of the grids at this position in the world.
        /// </summary>
        /// <param name="worldPos">The location of the tile in world coordinates.</param>
        /// <returns></returns>
        public IEnumerable<IMapGrid> FindGridsAt(Vector2 worldPos)
        {
            var gridList = new List<MapGrid>();
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(worldPos))
                    gridList.Add(kvGrid.Value);
            return gridList;
        }

        /// <summary>
        ///     Finds all grids that intersect the rectangle in the world.
        /// </summary>
        /// <param name="worldArea">The are in world coordinates to search.</param>
        /// <returns></returns>
        public IEnumerable<IMapGrid> FindGridsIntersecting(Box2 worldArea)
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
