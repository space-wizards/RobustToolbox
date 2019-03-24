using SS14.Client.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Map
{
    /// <summary>
    ///     Stores a texture atlas of all the tile definitions for efficient rendering.
    /// </summary>
    internal interface IClydeTileDefinitionManager : ITileDefinitionManager
    {
        /// <summary>
        ///     The texture atlas containing all the tiles.
        /// </summary>
        Texture TileTextureAtlas { get; }

        /// <summary>
        ///     Gets the region inside the texture atlas to use to draw a tile.
        /// </summary>
        /// <returns>If null, do not draw the tile at all.</returns>
        UIBox2? TileAtlasRegion(Tile tile);
    }
}
