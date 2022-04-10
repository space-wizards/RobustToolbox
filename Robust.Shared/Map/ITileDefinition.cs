namespace Robust.Shared.Map
{
    /// <summary>
    ///     The definition (template) for a grid tile.
    /// </summary>
    public interface ITileDefinition
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
        ///     Internal name of the definition.
        /// </summary>
        string ID { get; }

        /// <summary>
        ///     The name of the sprite to draw.
        /// </summary>
        string SpriteName { get; }

        /// <summary>
        ///     Path to the folder where the tile sprite is contained.
        ///     The texture dimensions should be PixelsPerMeter x (PixelsPerMeter * Variants).
        ///     This is likely 32 x (32 * variants) if you're working on Space Station 14.
        /// </summary>
        string Path { get; }

        /// <summary>
        ///     Physics objects that are interacting on this tile are slowed down by this float.
        /// </summary>
        float Friction { get; }

        /// <summary>
        ///     Number of variants this tile has. ALSO DETERMINES THE EXPECTED INPUT TEXTURE SIZE.
        /// </summary>
        byte Variants { get; }

        /// <summary>
        ///     Assign a new value to <see cref="TileId"/>, used when registering the tile definition.
        /// </summary>
        /// <param name="id">The new tile ID for this tile definition.</param>
        void AssignTileId(ushort id);
    }
}
