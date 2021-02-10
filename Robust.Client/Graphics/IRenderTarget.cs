using System;
using Robust.Shared.Maths;

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
    }
}
