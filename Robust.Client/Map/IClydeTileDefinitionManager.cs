using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Map
{
    /// <summary>
    ///     Stores a texture atlas of all the tile definitions for efficient rendering.
    /// </summary>
    internal interface IClydeTileDefinitionManager : ITileDefinitionManager
    {
        Box2 ErrorTileRegion { get; }

        /// <summary>
        ///     The texture atlas containing all the tiles.
        /// </summary>
        Texture TileTextureAtlas { get; }

        /// <summary>
        ///     Gets the region inside the texture atlas to use to draw a tile.
        /// </summary>
        /// <returns>If null, do not draw the tile at all.</returns>
        Box2[]? TileAtlasRegion(Tile tile);

        /// <summary>
        ///     Gets the region inside the texture atlas to use to draw a tile type.
        /// </summary>
        /// <returns>If null, do not draw the tile at all.</returns>
        Box2[]? TileAtlasRegion(int tileType);
    }
}
