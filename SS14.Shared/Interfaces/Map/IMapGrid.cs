using OpenTK;
using System;
using System.Collections.Generic;
using SS14.Shared.Map;

namespace SS14.Shared.Interfaces.Map
{
    /// <summary>
    ///     This is a collection of tiles in a grid format.
    /// </summary>
    public interface IMapGrid : IDisposable
    {
        /// <summary>
        ///     The bounding box of the grid in world coordinates.
        /// </summary>
        Box2 AABBWorld { get; }

        /// <summary>
        ///     The length of a side of the square chunk in number of tiles.
        /// </summary>
        ushort ChunkSize { get; }

        /// <summary>
        ///     The origin of the grid in world coordinates. Make sure to set this!
        /// </summary>
        Vector2 WorldPosition { get; set; }

        #region TileAccess

        /// <summary>
        ///     Gets a tile a the given world coordinates. This will not create a new chunk.
        /// </summary>
        /// <param name="worldPos">The location of the tile in world coordinates.</param>
        /// <returns>The tile at the world coordinates.</returns>
        TileRef GetTile(Vector2 worldPos);

        /// <summary>
        ///     Gets a tile a the given world coordinates. This will not create a new chunk.
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
        ///     Replaces a single tile inside of the grid.
        /// </summary>
        /// <param name="xWorld">The X coordinate of the tile in the world.</param>
        /// <param name="yWorld">The Y coordinate of the tile in the world.</param>
        /// <param name="tile">The new tile to insert.</param>
        void SetTile(float xWorld, float yWorld, Tile tile);

        /// <summary>
        ///     Replaces a single tile inside of the grid.
        /// </summary>
        /// <param name="worldPos">The location of the tile in global world coordinates.</param>
        /// <param name="tile">The new tile to insert.</param>
        void SetTile(Vector2 worldPos, Tile tile);

        /// <summary>
        ///     Replaces a single tile inside of the grid.
        /// </summary>
        /// <param name="xIndex">The local x tile index inside of the grid.</param>
        /// <param name="yIndex">The local y tile index inside of the grid.</param>
        /// <param name="tile">The tile to insert at the coordinates.</param>
        void SetTile(int xIndex, int yIndex, Tile tile);

        /// <summary>
        ///     Modifies a single tile inside of the chunk.
        /// </summary>
        /// <param name="xWorld">The X coordinate of the tile in the world.</param>
        /// <param name="yWorld">The Y coordinate of the tile in the world.</param>
        /// <param name="tileId">The new internal ID of the tile.</param>
        /// <param name="tileData">The new data of the tile.</param>
        void SetTile(float xWorld, float yWorld, ushort tileId, ushort tileData = 0);

        /// <summary>
        ///     Returns all tiles inside the area that match the predicate.
        /// </summary>
        /// <param name="worldArea">An area in the world to search for tiles.</param>
        /// <param name="ignoreEmpty">Will empty tiles be returned?</param>
        /// <param name="predicate">Optional predicate to filter the files.</param>
        /// <returns></returns>
        IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true, Predicate<TileRef> predicate = null);

        #endregion

        #region ChunkAccess

        /// <summary>
        ///     The total number of chunks contained on this grid.
        /// </summary>
        int ChunkCount { get; }

        /// <summary>
        ///     Returns the chunk at the given indices. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="xIndex">The X index of the chunk in this grid.</param>
        /// <param name="yIndex">The Y index of the chunk in this grid.</param>
        /// <returns>The existing or new chunk.</returns>
        IMapChunk GetChunk(int xIndex, int yIndex);

        /// <summary>
        ///     Returns the chunk at the given indices. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="chunkIndices">The indices of the chunk in this grid.</param>
        /// <returns>The existing or new chunk.</returns>
        IMapChunk GetChunk(MapGrid.Indices chunkIndices);

        /// <summary>
        ///     Returns all chunks in this grid. This will not generate new chunks.
        /// </summary>
        /// <returns>All chunks in the grid.</returns>
        IEnumerable<IMapChunk> GetMapChunks();

        #endregion

        #region Transforms

        /// <summary>
        ///     Transforms world-space coordinates from the global origin to the grid local origin.
        /// </summary>
        /// <param name="posWorld">The world-space coordinates with global origin.</param>
        /// <returns>The world-space coordinates with local origin.</returns>
        Vector2 WorldToLocal(Vector2 posWorld);

        /// <summary>
        ///     Transforms world-space coordinates from the local grid origin to the global origin.
        /// </summary>
        /// <param name="posLocal">The world-space coordinates with local grid origin.</param>
        /// <returns>The world-space coordinates with global origin.</returns>
        Vector2 LocalToWorld(Vector2 posLocal);


        /// <summary>
        ///     Transforms grid-space tile indices to local coordinates.
        /// </summary>
        /// <param name="gridTile"></param>
        /// <returns></returns>
        Vector2 GridTileToLocal(MapGrid.Indices gridTile);

        /// <summary>
        ///     Transforms grid-space tile indices to world coordinates.
        /// </summary>
        /// <param name="gridTile">The Grid Tile indices.</param>
        /// <returns></returns>
        Vector2 GridTileToWorld(MapGrid.Indices gridTile);

        #endregion
    }
}
