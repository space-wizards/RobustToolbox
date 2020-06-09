using System;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Shared;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Robust.Client.Interfaces.Placement
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

        void BeginPlacing(PlacementInformation info);
        void Clear();
        void ToggleEraser();

        void FrameUpdate(FrameEventArgs e);
    }
}
