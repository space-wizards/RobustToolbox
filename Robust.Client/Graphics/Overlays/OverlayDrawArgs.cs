using JetBrains.Annotations;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Parameters passed to <see cref="Overlay.Draw"/>.
    /// </summary>
    [PublicAPI]
    public readonly ref struct OverlayDrawArgs
    {
        /// <summary>
        ///     The overlay space that currently is being rendered for.
        /// </summary>
        public readonly OverlaySpace Space;

        /// <summary>
        ///     The viewport control that is rendering this viewport.
        ///     Not always available.
        /// </summary>
        public readonly IViewportControl? ViewportControl;

        /// <summary>
        ///     The viewport that is rendering this viewport.
        /// </summary>
        public readonly IClydeViewport Viewport;

        /// <summary>
        ///     The drawing handle that you can draw with.
        /// </summary>
        public readonly DrawingHandleBase DrawingHandle;

        /// <summary>
        ///     The screen-space coordinates available to render within.
        ///     Relevant for screen-space overlay rendering.
        /// </summary>
        public readonly UIBox2i ViewportBounds;

        /// <summary>
        /// <see cref="MapId"/> of the viewport's eye.
        /// </summary>
        public readonly MapId MapId;

        /// <summary>
        ///     AABB enclosing the area visible in the viewport.
        /// </summary>
        public readonly Box2 WorldAABB;

        /// <summary>
        ///     <see cref="Box2Rotated"/> of the area visible in the viewport.
        /// </summary>
        public readonly Box2Rotated WorldBounds;

        public DrawingHandleScreen ScreenHandle => (DrawingHandleScreen) DrawingHandle;
        public DrawingHandleWorld WorldHandle => (DrawingHandleWorld) DrawingHandle;

        public OverlayDrawArgs(
            OverlaySpace space,
            IViewportControl? viewportControl,
            IClydeViewport viewport,
            DrawingHandleBase drawingHandle,
            in UIBox2i viewportBounds,
            in MapId mapId,
            in Box2 worldAabb,
            in Box2Rotated worldBounds)
        {
            Space = space;
            ViewportControl = viewportControl;
            Viewport = viewport;
            DrawingHandle = drawingHandle;
            ViewportBounds = viewportBounds;
            MapId = mapId;
            WorldAABB = worldAabb;
            WorldBounds = worldBounds;
        }
    }
}
