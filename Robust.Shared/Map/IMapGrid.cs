using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
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
        ///     True if we are the default grid of our map.
        /// </summary>
        bool IsDefaultGrid { get; }

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
        TileRef GetTileRef(GridCoordinates worldPos);

        /// <summary>
        ///     Gets a tile a the given grid indices. This will not create a new chunk.
        /// </summary>
        /// <param name="tileCoordinates">The location of the tile in coordinates.</param>
        /// <returns>The tile at the tile coordinates.</returns>
        TileRef GetTileRef(MapIndices tileCoordinates);

        /// <summary>
        ///     Returns all tiles in the grid, in row-major order [xTileIndex, yTileIndex].
        /// </summary>
        /// <returns>All tiles in the chunk.</returns>
        IEnumerable<TileRef> GetAllTiles(bool ignoreSpace = true);

        /// <summary>
        ///     Replaces a single tile inside of the grid.
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="tile">The tile to insert at the coordinates.</param>
        void SetTile(GridCoordinates worldPos, Tile tile);

        /// <summary>
        ///     Modifies a single tile inside of the chunk.
        /// </summary>
        /// <param name="gridIndices"></param>
        /// <param name="tile">The tile to insert at the coordinates.</param>
        void SetTile(MapIndices gridIndices, Tile tile);

        /// <summary>
        ///     Returns all tiles inside the area that match the predicate.
        /// </summary>
        /// <param name="worldArea">An area in the world to search for tiles.</param>
        /// <param name="ignoreEmpty">Will empty tiles be returned?</param>
        /// <param name="predicate">Optional predicate to filter the files.</param>
        /// <returns></returns>
        IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true, Predicate<TileRef> predicate = null);

        IEnumerable<TileRef> GetTilesIntersecting(Circle worldArea, bool ignoreEmpty = true, Predicate<TileRef> predicate = null);

        #endregion TileAccess

        #region SnapGridAccess

        IEnumerable<SnapGridComponent> GetSnapGridCell(GridCoordinates worldPos, SnapGridOffset offset);
        IEnumerable<SnapGridComponent> GetSnapGridCell(MapIndices pos, SnapGridOffset offset);

        MapIndices SnapGridCellFor(GridCoordinates worldPos, SnapGridOffset offset);

        void AddToSnapGridCell(MapIndices pos, SnapGridOffset offset, SnapGridComponent snap);
        void AddToSnapGridCell(GridCoordinates worldPos, SnapGridOffset offset, SnapGridComponent snap);
        void RemoveFromSnapGridCell(MapIndices pos, SnapGridOffset offset, SnapGridComponent snap);
        void RemoveFromSnapGridCell(GridCoordinates worldPos, SnapGridOffset offset, SnapGridComponent snap);

        #endregion SnapGridAccess
        
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
        GridCoordinates LocalToWorld(GridCoordinates posLocal);

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
        MapIndices WorldToTile(Vector2 posWorld);

        /// <summary>
        ///     Transforms grid-space tile indices to local coordinates.
        ///     The resulting coordinates are centered on the tile.
        /// </summary>
        /// <param name="gridTile"></param>
        /// <returns></returns>
        GridCoordinates GridTileToLocal(MapIndices gridTile);

        /// <summary>
        ///     Transforms grid indices into a tile reference, returns false if no tile is found.
        /// </summary>
        /// <param name="indices">The Grid Tile indices.</param>
        /// <param name="tile"></param>
        /// <returns></returns>
        bool TryGetTileRef(MapIndices indices, out TileRef tile);

        /// <summary>
        /// Transforms grid tile indices to chunk indices.
        /// </summary>
        MapIndices GridTileToChunkIndices(MapIndices gridTile);

        /// <summary>
        /// Transforms local grid coordinates to chunk indices.
        /// </summary>
        MapIndices LocalToChunkIndices(GridCoordinates posWorld);

        #endregion Transforms

        #region Collision

        bool CollidesWithGrid(MapIndices indices);

        #endregion
    }
}
