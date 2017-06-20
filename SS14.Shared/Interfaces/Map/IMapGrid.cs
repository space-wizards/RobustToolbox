using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;
using SS14.Shared.Map;

namespace SS14.Shared.Interfaces.Map
{
    public interface IMapGrid : IDisposable
    {
        /// <summary>
        /// The bounding box of the grid in world coordinates.
        /// </summary>
        FloatRect AABBWorld { get; }

        /// <summary>
        /// The length of a side of the square chunk in number of tiles.
        /// </summary>
        ushort ChunkSize { get; }

        /// <summary>
        /// The origin of the grid in world coordinates. Make sure to set this!
        /// </summary>
        Vector2f WorldPosition { get; set; }

        #region TileAccess

        /// <summary>
        /// Gets a tile a the given world coordinates. This will not create a new chunk.
        /// </summary>
        /// <param name="posWorld">The location of the tile in world coordinates.</param>
        /// <returns>The tile at the world coordinates.</returns>
        TileRef GetTile(Vector2f posWorld);

        /// <summary>
        /// Gets a tile a the given world coordinates. This will not create a new chunk.
        /// </summary>
        /// <param name="xWorld">The X coordinate of the tile in the world.</param>
        /// <param name="yWorld">The Y coordinate of the tile in the world.</param>
        /// <returns>The tile at the world coordinates.</returns>
        TileRef GetTile(float xWorld, float yWorld);

        /// <summary>
        ///     Returns all tiles in the grid, in row-major order [xTileIndex, yTileIndex].
        /// </summary>
        /// <returns>All tiles in the chunk.</returns>  
        IEnumerable<TileRef> GetAllTiles(bool ignoreSpace = true);

        /// <summary>
        /// Replaces a single tile inside of the grid.
        /// </summary>
        /// <param name="xWorld">The X coordinate of the tile in the world.</param>
        /// <param name="yWorld">The Y coordinate of the tile in the world.</param>
        /// <param name="tile">The new tile to insert.</param>
        void SetTile(float xWorld, float yWorld, Tile tile);

        /// <summary>
        /// Replaces a single tile inside of the grid.
        /// </summary>
        /// <param name="posWorld">The location of the tile in global world coordinates.</param>
        /// <param name="tile">The new tile to insert.</param>
        void SetTile(Vector2f posWorld, Tile tile);

        /// <summary>
        /// Replaces a single tile inside of the grid.
        /// </summary>
        /// <param name="xIndex">The local x tile index inside of the grid.</param>
        /// <param name="yIndex">The local y tile index inside of the grid.</param>
        /// <param name="tile">The tile to insert at the coordinates.</param>
        void SetTile(int xIndex, int yIndex, Tile tile);

        /// <summary>
        /// Modifies a single tile inside of the chunk.
        /// </summary>
        /// <param name="xWorld">The X coordinate of the tile in the world.</param>
        /// <param name="yWorld">The Y coordinate of the tile in the world.</param>
        /// <param name="tileId">The new internal ID of the tile.</param>
        /// <param name="tileData">The new data of the tile.</param>
        void SetTile(float xWorld, float yWorld, ushort tileId, ushort tileData = 0);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="areaWorld"></param>
        /// <param name="ignoreEmpty"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IEnumerable<TileRef> GetTilesIntersecting(FloatRect areaWorld, bool ignoreEmpty = true, Predicate<TileRef> predicate = null);

        #endregion

        #region ChunkAccess

        int ChunkCount { get; }

        /// <summary>
        ///     Returns the chunk at the given position on the given map. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="xIndex"></param>
        /// <param name="yIndex"></param>
        /// <returns></returns>
        IMapChunk GetChunk(int xIndex, int yIndex);

        /// <summary>
        ///     Returns the chunk at the given position on the given map. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="chunkIndices"></param>
        /// <returns></returns>
        IMapChunk GetChunk(MapGrid.Indices chunkIndices);

        /// <summary>
        /// Returns all map chunks in a grid. This will not generate new chunks.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IMapChunk> GetMapChunks();

        #endregion

        #region Transforms

        /// <summary>
        /// Transforms world-space coordinates from the global origin to the grid local origin.
        /// </summary>
        /// <param name="posWorld">The world-space coordinates with global origin.</param>
        /// <returns>The world-space coordinates with local origin.</returns>
        Vector2f WorldToLocal(Vector2f posWorld);

        /// <summary>
        /// Transforms world-space coordinates from the local grid origin to the global origin.
        /// </summary>
        /// <param name="posLocal">The world-space coordinates with local grid origin.</param>
        /// <returns>The world-space coordinates with global origin.</returns>
        Vector2f LocalToWorld(Vector2f posLocal);

        #endregion
    }
}
