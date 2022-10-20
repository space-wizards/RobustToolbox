using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     The definition (template) for a grid tile.
    /// </summary>
    public interface ITileDefinition
    {
        /// <summary>
        ///     The name of the definition. This is user facing.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Internal name of the definition.
        /// </summary>
        string ID { get; }

        /// <summary>
        ///     The path of the sprite to draw.
        /// </summary>
        ResourcePath? Sprite { get; }

        /// <summary>
        ///     Physics objects that are interacting on this tile are slowed down by this float.
        /// </summary>
        float Friction { get; }

        /// <summary>
        ///     Number of variants this tile has. ALSO DETERMINES THE EXPECTED INPUT TEXTURE SIZE.
        /// </summary>
        byte Variants { get; }
    }
}
