using System;
using System.Collections.Generic;
using System.Text;

namespace Robust.Shared.Enums {
    /// <summary>
    ///     Determines in which canvas layers an overlay gets drawn.
    /// </summary>
    [Flags]
    public enum OverlaySpace {
        /// <summary>
        ///     Used for matching bit flags.
        /// </summary>
        None = 0b0000,

        /// <summary>
        ///     This overlay will be drawn in the UI root, thus being in screen space.
        /// </summary>
        ScreenSpace = 0b0001,

        /// <summary>
        ///     This overlay will be drawn in the world root, thus being in world space.
        /// </summary>
        WorldSpace = 0b0010,

        /// <summary>
        ///     This overlay will be drawn in worldspace, but a stencil equivalent to the FOV will be applied.
        /// </summary>
        WorldSpaceFOVStencil = 0b0100,

        /// <summary>
        ///     Drawn in screen coordinates, but behind the world.
        /// </summary>
        ScreenSpaceBelowWorld = 0b1000,
    }
}
