using System;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Robust.Client.Placement
{
    public interface IPlacementManager
    {
        void Initialize();
        bool IsActive { get; }
        bool Eraser { get; }
        PlacementMode? CurrentMode { get; set; }
        PlacementInformation? CurrentPermission { get; set; }

        /// <summary>
        /// Gets called when the PlacementManager changed its build/erase mode or when the hijacks changed
        /// </summary>
        event EventHandler PlacementChanged;

        void BeginPlacing(PlacementInformation info, PlacementHijack? hijack = null);
        void Clear();
        void ToggleEraser();
        void ToggleEraserHijacked(PlacementHijack hijack);

        void FrameUpdate(FrameEventArgs e);
    }
}
