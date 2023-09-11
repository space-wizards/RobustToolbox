using JetBrains.Annotations;

namespace Robust.Shared.Graphics
{
    /// <summary>
    ///     Controls behavior when reading texture coordinates outside 0-1, which usually wraps the texture somehow.
    /// </summary>
    [PublicAPI]
    public enum TextureWrapMode : byte
    {
        /// <summary>
        ///     Do not wrap, instead clamp to edge.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Repeat the texture.
        /// </summary>
        Repeat,

        /// <summary>
        ///     Repeat the texture mirrored.
        /// </summary>
        MirroredRepeat,
    }
}
