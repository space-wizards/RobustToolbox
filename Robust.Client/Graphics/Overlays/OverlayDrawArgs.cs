using JetBrains.Annotations;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
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

        public readonly EntityUid MapUid;

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

        public readonly IRenderHandle RenderHandle;

        public DrawingHandleScreen ScreenHandle => (DrawingHandleScreen) DrawingHandle;
        public DrawingHandleWorld WorldHandle => (DrawingHandleWorld) DrawingHandle;

        internal OverlayDrawArgs(
            OverlaySpace space,
            IViewportControl? viewportControl,
            IClydeViewport viewport,
            IRenderHandle renderHandle,
            in UIBox2i viewportBounds,
            in EntityUid mapUid,
            in MapId mapId,
            in Box2 worldAabb,
            in Box2Rotated worldBounds)
        {
            DrawingHandle = space is OverlaySpace.ScreenSpace or OverlaySpace.ScreenSpaceBelowWorld
                ? renderHandle.DrawingHandleScreen
                : renderHandle.DrawingHandleWorld;

            Space = space;
            ViewportControl = viewportControl;
            Viewport = viewport;
            RenderHandle = renderHandle;
            ViewportBounds = viewportBounds;
            MapUid = mapUid;
            MapId = mapId;
            WorldAABB = worldAabb;
            WorldBounds = worldBounds;
        }
    }
}
