using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.Interfaces.Graphics
{
    /// <summary>
    ///     Represents a render target that can be drawn to.
    /// </summary>
    public interface IRenderTarget
    {
        /// <summary>
        ///     The size of the render target, in pixels.
        /// </summary>
        Vector2i Size { get; }

        /// <summary>
        ///     A texture that contains the contents of the render target.
        /// </summary>
        Texture Texture { get; }

        /// <summary>
        ///     Delete this render target and its backing textures.
        /// </summary>
        void Delete();
    }
}