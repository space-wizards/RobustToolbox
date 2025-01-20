using System;
using System.Collections.Generic;
using System.Text;

namespace Robust.Shared.Enums
{
    /// <summary>
    ///     Determines in which canvas layers an overlay gets drawn.
    /// </summary>
    [Flags]
    public enum OverlaySpace : ushort
    {
        /// <summary>
        ///     Used for matching bit flags.
        /// </summary>
        None = 0,

        /// <summary>
        ///     This overlay will be drawn in screen coordinates in the UI space above the world.
        /// </summary>
        ScreenSpace = 1 << 1,

        /// <summary>
        ///     This overlay will be drawn above entities, lighting, and FOV.
        /// </summary>
        WorldSpace = 1 << 2,

        /// <summary>
        ///     This overlay will be drawn beneath FOV and lighting, but above entities.
        /// </summary>
        WorldSpaceBelowFOV = 1 << 3,

        /// <summary>
        ///     This overlay will be along with entities, with the order depending on overlay's ZOrder and sprite's
        ///     DrawDepth.
        /// </summary>
        WorldSpaceEntities = 1 << 4,

        /// <summary>
        ///     Drawn after every grid.
        /// </summary>
        WorldSpaceGrids = 1 << 5,

        /// <summary>
        ///     This overlay will be drawn beneath entities, lighting, and FOV; above grids.
        /// </summary>
        WorldSpaceBelowEntities = 1 << 6,

        /// <summary>
        ///     This overlay will be drawn in screen coordinates behind the world.
        /// </summary>
        ScreenSpaceBelowWorld = 1 << 7,

        /// <summary>
        ///     Overlay will be rendered below grids, entities, and everything else. In world space.
        /// </summary>
        WorldSpaceBelowWorld = 1 << 8,

        /// <summary>
        /// Called after GLClear but before FOV applied to the lighting buffer.
        /// </summary>
        BeforeLighting = 1 << 9,
    }
}
