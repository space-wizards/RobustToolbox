using System;
using SS14.Client.Input;
using SS14.Client.Placement;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.Map;

namespace SS14.Client.Interfaces.Placement
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
        void Render();
        void Clear();
        void ToggleEraser();

        void FrameUpdate(RenderFrameEventArgs e);
    }
}
