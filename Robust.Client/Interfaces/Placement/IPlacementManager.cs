using System;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Shared;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Robust.Client.Interfaces.Placement
{
    public interface IPlacementManager
    {
        void Initialize();
        bool IsActive { get; }
        bool Eraser { get; }
        PlacementMode CurrentMode { get; set; }
        PlacementInformation CurrentPermission { get; set; }

        event EventHandler PlacementCanceled;

        void BeginPlacing(PlacementInformation info);
        void Clear();
        void ToggleEraser();

        void FrameUpdate(RenderFrameEventArgs e);
    }
}
