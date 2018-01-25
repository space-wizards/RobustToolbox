using System;
using System.Collections.Generic;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Shared.Interfaces.Map
{
    /// <summary>
    ///     This is a collection of tiles in a grid format.
    /// </summary>
    public interface IMapGrid : IDisposable
    {
        /// <summary>
        ///     The integer ID of the map this grid is located within
        /// </summary>
        MapId MapID { get; }

        GridId Index { get; }

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        ushort TileSize { get; }

        /// <summary>
        ///     The bounding box of the grid in world coordinates.
        /// </summary>
        Box2 AABBWorld { get; }

        /// <summary>
        ///     The length of a side of the square chunk in number of tiles.
        /// </summary>
        ushort ChunkSize { get; }

        /// <summary>
        ///     The distance between the snap grid, between each center snap and between each offset snap grid location
        /// </summary>
        float SnapSize { get; }

        /// <summary>
        ///     The origin of the grid in world coordinates. Make sure to set this!
        /// </summary>
        Vector2 WorldPosition { get; set; }

        /// <summary>
        ///     Is this located at a position on the center grid of snap positions, accepts local coordinates
        /// </summary>
        bool OnSnapCenter(Vector2 position);

        /// <summary>
        ///     Is this located at a position on the border grid of snap positions, accepts local coordinates
        /// </summary>
        bool OnSnapBorder(Vector2 position);

        #region TileAccess

        /// <summary>
        ///     Gets a tile a the given world coordinates. This will not create a new chunk.
        /// </summary>
        /// <param name="worldPos">The location of the tile in coordinates.</param>
        /// <returns>The tile at the world coordinates.</returns>
        TileRef GetTile(LocalCoordinates posWorld);

        /// <summary>
        ///     Returns all tiles in the grid, in row-major order [xTileIndex, yTileIndex].
        /// </summary>
        /// <returns>All tiles in the chunk.</returns>
        IEnumerable<TileRef> GetAllTiles(bool ignoreSpace = true);

        /// <summary>
        ///     Replaces a single tile inside of the grid.
        /// </summary>
        /// <param name="xIndex">The local x tile index inside of the grid.</param>
        /// <param name="yIndex">The local y tile index inside of the grid.</param>
        /// <param name="tile">The tile to insert at the coordinates.</param>
        void SetTile(LocalCoordinates posWorld, Tile tile);

        /// <summary>
        ///     Modifies a single tile inside of the chunk.
        /// </summary>
        /// <param name="xWorld">The X coordinate of the tile in the world.</param>
        /// <param name="yWorld">The Y coordinate of the tile in the world.</param>
        /// <param name="tileId">The new internal ID of the tile.</param>
        /// <param name="tileData">The new data of the tile.</param>
        void SetTile(LocalCoordinates posWorld, ushort tileId, ushort tileData = 0);

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
        LocalCoordinates LocalToWorld(LocalCoordinates posLocal);

        /// <summary>
        ///     Transforms local vectors into world space vectors
        /// </summary>
        /// <param name="posLocal">The local vector with this grid as origin.</param>
        /// <returns>The world-space vector with global origin.</returns>
        Vector2 ConvertToWorld(Vector2 localpos);

        /// <summary>
        ///     Transforms grid-space tile indices to local coordinates.
        /// </summary>
        /// <param name="gridTile"></param>
        /// <returns></returns>
        LocalCoordinates GridTileToLocal(MapGrid.Indices gridTile);

        /// <summary>
        ///     Transforms grid indices into an outvar tile, returns false if no tile is found
        /// </summary>
        /// <param name="gridTile">The Grid Tile indices.</param>
        /// <returns></returns>
        bool IndicesToTile(MapGrid.Indices indices, out TileRef tile);

        #endregion
    }
}
