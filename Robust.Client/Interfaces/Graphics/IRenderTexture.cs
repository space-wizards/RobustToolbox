using Robust.Client.Graphics;

namespace Robust.Client.Interfaces.Graphics
{
    /// <summary>
    ///     A render target that renders into a texture that can be re-used later.
    /// </summary>
    public interface IRenderTexture : IRenderTarget
    {
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
