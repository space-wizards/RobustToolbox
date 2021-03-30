using System;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     A viewport is an API for rendering a section of the game map centered around an eye,
    ///     complete with lighting, FOV and grid rendering.
    /// </summary>
    public interface IClydeViewport : IDisposable
    {
        /// <summary>
        ///     The render target that is rendered to when rendering this viewport.
        /// </summary>
        IRenderTexture RenderTarget { get; }
        IEye? Eye { get; set; }
        Vector2i Size { get; }

        /// <summary>
        ///     If true, <see cref="Render"/> will be automatically called at the start of the frame.
        /// </summary>
        bool AutomaticRender { get; set; }

        /// <summary>
        ///     Render the state of the world in this viewport, updating the texture inside the render target.
        /// </summary>
        void Render();
        Vector2 WorldToLocal(Vector2 point);

        void RenderScreenOverlaysBelow(DrawingHandleScreen handle);
        void RenderScreenOverlaysAbove(DrawingHandleScreen handle);
    }
}
