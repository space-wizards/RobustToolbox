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
        None = 0b000000,

        /// <summary>
        ///     This overlay will be drawn in screen coordinates in the UI space above the world.
        /// </summary>
        ScreenSpace = 0b000001,

        /// <summary>
        ///     This overlay will be drawn above entities, lighting, and FOV.
        /// </summary>
        WorldSpace = 0b000100,

        /// <summary>
        ///     This overlay will be drawn beneath FOV; above lighting and entities.
        /// </summary>
        WorldSpaceBelowFOV = 0b001000,

        /// <summary>
        ///     This overlay will be drawn beneath entities, lighting, and FOV; above grids.
        /// </summary>
        WorldSpaceBelowEntities = 0b010000,

        /// <summary>
        ///     This overlay will be drawn in screen coordinates behind the world.
        /// </summary>
        ScreenSpaceBelowWorld = 0b100000,
    }
}
