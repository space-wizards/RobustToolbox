using System;
using Robust.Shared.Maths;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Represents something that can be rendered to.
    /// </summary>
    public interface IRenderTarget : IDisposable
    {
        /// <summary>
        ///     The size of the render target, in physical pixels.
        /// </summary>
        Vector2i Size { get; }

        void CopyPixelsToMemory<T>(CopyPixelsDelegate<T> callback, UIBox2i? subRegion = null) where T : unmanaged, IPixel<T>;
    }
}
