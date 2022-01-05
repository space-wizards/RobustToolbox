using System;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Map;
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
        /// Color to clear the render target to before rendering. If null, no clearing will happen.
        /// </summary>
        Color? ClearColor { get; set; }

        /// <summary>
        ///     This is, effectively, a multiplier to the eye's zoom.
        /// </summary>
        Vector2 RenderScale { get; set; }

        /// <summary>
        ///     If true, <see cref="Render"/> will be automatically called at the start of the frame.
        /// </summary>
        bool AutomaticRender { get; set; }

        /// <summary>
        ///     Render the state of the world in this viewport, updating the texture inside the render target.
        /// </summary>
        void Render();

        /// <summary>
        ///     Converts a point in the viewport's screen to world coordinates.
        /// </summary>
        MapCoordinates LocalToWorld(Vector2 point);

        /// <summary>
        ///     Converts a point in world-space to the viewport's screen coordinates.
        /// </summary>
        Vector2 WorldToLocal(Vector2 point);

        /// <summary>
        ///     Draw below screen-space overlays for this viewport in UI space.
        /// </summary>
        /// <param name="handle">The drawing handle to draw with.</param>
        /// <param name="control">The control rendering.</param>
        /// <param name="viewportBounds">
        ///     Absolute screen-space bounds to draw the control at.
        ///     Not relative to the current transform of <see cref="handle"/>.
        /// </param>
        public void RenderScreenOverlaysBelow(
            DrawingHandleScreen handle,
            IViewportControl control,
            in UIBox2i viewportBounds);

        /// <summary>
        ///     Draw above screen-space overlays for this viewport in UI space.
        /// </summary>
        /// <param name="handle">The drawing handle to draw with.</param>
        /// <param name="control">The control rendering.</param>
        /// <param name="viewportBounds">
        ///     Absolute screen-space bounds to draw the control at.
        ///     Not relative to the current transform of <see cref="handle"/>.
        /// </param>
        public void RenderScreenOverlaysAbove(
            DrawingHandleScreen handle,
            IViewportControl control,
            in UIBox2i viewportBounds);
    }
}
