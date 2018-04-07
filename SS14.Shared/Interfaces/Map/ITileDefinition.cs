using SS14.Shared.Map;

namespace SS14.Shared.Interfaces.Map
{
    /// <summary>
    ///     The definition (template) for a grid tile.
    /// </summary>
    public interface ITileDefinition
    {
        /// <summary>
        ///     The internal ID of the tile definition.
        /// </summary>
        ushort TileId { get; }

        /// <summary>
        ///     The name of the definition.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// </summary>
        bool IsConnectingSprite { get; }

        /// <summary>
        ///     Is the tile nontransparent?
        /// </summary>
        bool IsOpaque { get; }

        /// <summary>
        ///     Are items stopped by this tile?
        /// </summary>
        bool IsCollidable { get; }

        /// <summary>
        ///     Can this tile contain a gas?
        /// </summary>
        bool IsGasVolume { get; }

        /// <summary>
        ///     Does this tile vent gas into space?
        /// </summary>
        bool IsVentedIntoSpace { get; }

        /// <summary>
        ///     Is this tile a floor?
        /// </summary>
        bool IsFloor { get; }

        /// <summary>
        ///     The name of the sprite to draw.
        /// </summary>
        string SpriteName { get; }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="data">Optional data to add to this tile.</param>
        /// <returns></returns>
        Tile Create(ushort data = 0);
    }
}
