using System;
using SS14.Client.Input;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;

namespace SS14.Client.Interfaces.Placement
{
    public interface IPlacementManager
    {
        void Initialize();
        bool IsActive { get; }
        bool Eraser { get; }

        event EventHandler PlacementCanceled;

        void HandleDeletion(IEntity entity);
        void HandlePlacement();
        void BeginPlacing(PlacementInformation info);
        void Render();
        void Clear();
        void ToggleEraser();
        void Rotate();

        bool MouseDown(MouseButtonEventArgs e);
        bool MouseUp(MouseButtonEventArgs e);
        void FrameUpdate(RenderFrameEventArgs e);
    }
}
