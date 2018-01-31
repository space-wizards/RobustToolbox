using SS14.Shared.Map;
using System.Collections.Generic;
using SS14.Shared.Maths;

namespace SS14.Shared.Interfaces.Map
{
    public interface IMap
    {
        MapId Index { get; }
        
        #region GridAccess

        /// <summary>
        ///     Creates a new empty grid with the given ID and optional chunk size. If a grid already
        ///     exists with the gridID, it is overwritten with the new grid.
        /// </summary>
        /// <param name="gridId">The id of the new grid to create.</param>
        /// <param name="chunkSize">Optional chunk size of the new grid.</param>
        /// <param name="snapSize">Optional size of the snap grid</param>
        /// <returns></returns>
        IMapGrid CreateGrid(GridId gridId, ushort chunkSize = 16, float snapSize = 1);

        /// <summary>
        ///     Checks if a grid exists with the given ID.
        /// </summary>
        /// <param name="gridId">The ID of the grid to check.</param>
        /// <returns></returns>
        bool GridExists(GridId gridId);

        /// <summary>
        ///     Gets the grid associated with the given grid ID. If the grid with the given ID does not exist, return null.
        /// </summary>
        /// <param name="gridId">The id of the grid to get.</param>
        /// <returns></returns>
        IMapGrid GetGrid(GridId gridId);

        /// <summary>
        ///     Alias of IMapManager.GetGrid(IMapManager.DefaultGridId);
        /// </summary>
        /// <returns></returns>
        IMapGrid GetDefaultGrid();

        /// <summary>
        ///     Deletes the grid associated with the given grid ID.
        /// </summary>
        /// <param name="gridId">The grid to remove.</param>
        void RemoveGrid(GridId gridId);

        /// <summary>
        ///     Finds all of the grids at this position in the world.
        /// </summary>
        /// <param name="worldPos">The location of the tile in world coordinates.</param>
        /// <returns></returns>
        IMapGrid FindGridAt(LocalCoordinates posWorld);

        /// <summary>
        ///     Finds all of the grids at this position in the world.
        /// </summary>
        /// <param name="worldPos">The location of the tile in world coordinates.</param>
        /// <returns></returns>
        IMapGrid FindGridAt(Maths.Vector2 worldPos);

        /// <summary>
        ///     Finds all grids that intersect the rectangle in the world.
        /// </summary>
        /// <param name="worldArea">The are in world coordinates to search.</param>
        /// <returns></returns>
        IEnumerable<IMapGrid> FindGridsIntersecting(Box2 worldArea);

        #endregion GridAccess

        IEnumerable<IMapGrid> GetAllGrids();
    }
}
