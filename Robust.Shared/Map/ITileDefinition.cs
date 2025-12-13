using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <summary>
    /// Determines the placement and selection of edge sprites for tiles
    /// </summary>
    public enum TileBordersMode : byte
    {
        /// <summary>
        /// Display any number of 8 border image files at boundaries with other tile types
        /// outside the bounds of this tile
        /// </summary>
        Exterior8Patch,
        /// <summary>
        /// Display any number of 8 border image files at boundaries with other tile types
        /// inside the bounds of this tile
        /// </summary>
        Interior8Patch,
        /// <summary>
        /// Tiles display up to 4 of 16 border image files at boundaries with other tile types
        /// inside the bounds of this tile
        /// </summary>
        Interior4Of16,
    }

    /// <summary>
    /// Key for edge sprites to be used in the <cref see="TileBordersMode.Interior4Of16" /> interior 4-of-16 mode
    /// </summary>
    public enum Interior4Of16Edge : byte
    {
        Full,
        SideNorthEast,
        SideNorthWest,
        SideSouthEast,
        SideSouthWest,
        SideNorthEastSouth,
        SideNorthEastWest,
        SideEastSouthWest,
        SideNorthSouthWest,
        SideNorth,
        SideEast,
        SideSouth,
        SideWest,
        CornerNorthEast,
        CornerSouthEast,
        CornerNorthWest,
        CornerSouthWest,
    }

    /// <summary>
    ///     The definition (template) for a grid tile.
    /// </summary>
    public interface ITileDefinition : IPrototype
    {
        /// <summary>
        ///     The numeric tile ID used to refer to this tile inside the map datastructure.
        /// </summary>
        ushort TileId { get; }

        /// <summary>
        ///     The name of the definition. This is user facing.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     The path of the sprite to draw.
        /// </summary>
        ResPath? Sprite { get; }

        /// <summary>
        ///     Possible sprites to use if we're neighboring another tile.
        /// </summary>
        Dictionary<Direction, ResPath> EdgeSprites { get; }

        /// <summary>
        /// Possible sprites to use if we're neighbouring another tile in the 4-of-16 mode
        /// </summary>
        Dictionary<Interior4Of16Edge, ResPath> Interior4Of16EdgeSprites { get; }

        /// <summary>
        ///     If the edge sprites should be drawn on the interior of the tile rather than the exterior
        /// </summary>
        TileBordersMode BordersMode => TileBordersMode.Exterior8Patch;

        /// <summary>
        ///     When drawing adjacent tiles that both specify edge sprites, the one with the higher priority
        ///     is always solely drawn.
        /// </summary>
        int EdgeSpritePriority { get; }

        /// <summary>
        ///     Physics objects that are interacting on this tile are slowed down by this float.
        /// </summary>
        float Friction { get; }

        /// <summary>
        ///     Number of variants this tile has. ALSO DETERMINES THE EXPECTED INPUT TEXTURE SIZE.
        /// </summary>
        byte Variants { get; }

        /// <summary>
        ///     Allows the tile to be rotated/mirrored when placed on a grid.
        /// </summary>
        bool AllowRotationMirror => false;

        /// <summary>
        ///     Assign a new value to <see cref="TileId"/>, used when registering the tile definition.
        /// </summary>
        /// <param name="id">The new tile ID for this tile definition.</param>
        void AssignTileId(ushort id);

        /// <summary>
        ///     Allows you to hide tiles from the tile spawn menu.
        /// </summary>
        bool EditorHidden => false;
    }
}
