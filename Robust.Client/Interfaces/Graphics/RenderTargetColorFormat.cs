namespace Robust.Client.Interfaces.Graphics
{
    /// <summary>
    ///     Formats for the color component of a render target.
    /// </summary>
    public enum RenderTargetColorFormat : byte
    {
        /// <summary>
        ///     8 bits per channel linear RGBA.
        /// </summary>
        Rgba8,

        /// <summary>
        ///     8 bits per channel sRGB with linear alpha channel.
        /// </summary>
        Rgba8Srgb,

        R32F,
        RG32F,

        /// <summary>
        ///     16 bits per channel floating point linear RGBA.
        /// </summary>
        Rgba16F,

        R11FG11FB10F,

        R8,
    }
}
