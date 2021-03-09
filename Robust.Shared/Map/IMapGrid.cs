using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     This is a collection of tiles in a grid format.
    /// </summary>
    [PublicAPI]
    public interface IMapGrid : IDisposable
    {
        /// <summary>
        ///     The integer ID of the map this grid is currently located within.
        /// </summary>
        MapId ParentMapId { get; set; }

        EntityUid GridEntityId { get; }

        /// <summary>
        ///     The identifier of this grid.
        /// </summary>
        GridId Index { get; }

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        ushort TileSize { get; }

        /// <summary>
        ///     The bounding box of the grid in world coordinates.
        /// </summary>
        Box2 WorldBounds { get; }

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
        ///     Whether or not this grid has gravity
        /// </summary>
        bool HasGravity { get; set; }

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
        /// <param name="coords">The location of the tile in coordinates.</param>
        /// <returns>The tile at the world coordinates.</returns>
        TileRef GetTileRef(EntityCoordinates coords);

        /// <summary>
        ///     Gets a tile a the given grid indices. This will not create a new chunk.
        /// </summary>
        /// <param name="tileCoordinates">The location of the tile in coordinates.</param>
        /// <returns>The tile at the tile coordinates.</returns>
        TileRef GetTileRef(Vector2i tileCoordinates);

        /// <summary>
        ///     Returns all tiles in the grid, in row-major order [xTileIndex, yTileIndex].
        /// </summary>
        /// <returns>All tiles in the chunk.</returns>
        IEnumerable<TileRef> GetAllTiles(bool ignoreSpace = true);

        /// <summary>
        ///     Replaces a single tile inside of the grid.
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="tile">The tile to insert at the coordinates.</param>
        void SetTile(EntityCoordinates coords, Tile tile);

        /// <summary>
        ///     Modifies a single tile inside of the chunk.
        /// </summary>
        /// <param name="gridIndices"></param>
        /// <param name="tile">The tile to insert at the coordinates.</param>
        void SetTile(Vector2i gridIndices, Tile tile);

        /// <summary>
        ///     Returns all tiles inside the area that match the predicate.
        /// </summary>
        /// <param name="worldArea">An area in the world to search for tiles.</param>
        /// <param name="ignoreEmpty">Will empty tiles be returned?</param>
        /// <param name="predicate">Optional predicate to filter the files.</param>
        /// <returns></returns>
        IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true, Predicate<TileRef>? predicate = null);

        IEnumerable<TileRef> GetTilesIntersecting(Circle worldArea, bool ignoreEmpty = true, Predicate<TileRef>? predicate = null);

        #endregion TileAccess

        #region SnapGridAccess

        IEnumerable<SnapGridComponent> GetSnapGridCell(EntityCoordinates coords, SnapGridOffset offset);
        IEnumerable<SnapGridComponent> GetSnapGridCell(Vector2i pos, SnapGridOffset offset);

        Vector2i SnapGridCellFor(EntityCoordinates coords, SnapGridOffset offset);
        Vector2i SnapGridCellFor(MapCoordinates worldPos, SnapGridOffset offset);
        Vector2i SnapGridCellFor(Vector2 localPos, SnapGridOffset offset);

        void AddToSnapGridCell(Vector2i pos, SnapGridOffset offset, SnapGridComponent snap);
        void AddToSnapGridCell(EntityCoordinates coords, SnapGridOffset offset, SnapGridComponent snap);
        void RemoveFromSnapGridCell(Vector2i pos, SnapGridOffset offset, SnapGridComponent snap);
        void RemoveFromSnapGridCell(EntityCoordinates coords, SnapGridOffset offset, SnapGridComponent snap);

        #endregion SnapGridAccess

        #region Transforms

        /// <summary>
        ///     Transforms EntityCoordinates to a local tile location.
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        Vector2i CoordinatesToTile(EntityCoordinates coords);

        /// <summary>
        ///     Transforms world-space coordinates from the global origin to the grid local origin.
        /// </summary>
        /// <param name="posWorld">The world-space coordinates with global origin.</param>
        /// <returns>The world-space coordinates with local origin.</returns>
        Vector2 WorldToLocal(Vector2 posWorld);

        /// <summary>
        /// Transforms map coordinates to grid coordinates.
        /// </summary>
        EntityCoordinates MapToGrid(MapCoordinates posWorld);

        /// <summary>
        ///     Transforms local vectors into world space vectors
        /// </summary>
        /// <param name="posLocal">The local vector with this grid as origin.</param>
        /// <returns>The world-space vector with global origin.</returns>
        Vector2 LocalToWorld(Vector2 posLocal);

        /// <summary>
        ///     Transforms World position into grid tile indices.
        /// </summary>
        /// <param name="posWorld">Position in the world.</param>
        /// <returns>Indices of a tile on the grid.</returns>
        Vector2i WorldToTile(Vector2 posWorld);

        /// <summary>
        ///     Transforms grid-space tile indices to local coordinates.
        ///     The resulting coordinates are centered on the tile.
        /// </summary>
        /// <param name="gridTile"></param>
        /// <returns></returns>
        EntityCoordinates GridTileToLocal(Vector2i gridTile);

        /// <summary>
        ///     Transforms grid-space tile indices to map coordinate position.
        ///     The resulting coordinates are centered on the tile.
        /// </summary>
        Vector2 GridTileToWorldPos(Vector2i gridTile);

        /// <summary>
        ///     Transforms grid-space tile indices to map coordinates.
        ///     The resulting coordinates are centered on the tile.
        /// </summary>
        MapCoordinates GridTileToWorld(Vector2i gridTile);

        /// <summary>
        ///     Transforms grid indices into a tile reference, returns false if no tile is found.
        /// </summary>
        /// <param name="indices">The Grid Tile indices.</param>
        /// <param name="tile"></param>
        /// <returns></returns>
        bool TryGetTileRef(Vector2i indices, out TileRef tile);

        /// <summary>
        ///     Transforms coordinates into a tile reference, returns false if no tile is found.
        /// </summary>
        /// <param name="coords">The coordinates.</param>
        /// <param name="tile"></param>
        /// <returns></returns>
        bool TryGetTileRef(EntityCoordinates coords, out TileRef tile);

        /// <summary>
        /// Transforms grid tile indices to chunk indices.
        /// </summary>
        Vector2i GridTileToChunkIndices(Vector2i gridTile);

        /// <summary>
        /// Transforms local grid coordinates to chunk indices.
        /// </summary>
        Vector2i LocalToChunkIndices(EntityCoordinates gridPos);

        #endregion Transforms

        #region Collision

        bool CollidesWithGrid(Vector2i indices);

        #endregion
    }
}
