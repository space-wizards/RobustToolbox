using Lidgren.Network;
using SS14.Client.Interfaces.Map;
using SS14.Shared;
using SS14.Shared.GameObjects;
using System;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Placement
{
    public interface IPlacementManager
    {
        Boolean IsActive { get; }
        Boolean Eraser { get; }

        event EventHandler PlacementCanceled;

        void HandleDeletion(Entity entity);
        void HandlePlacement();
        void BeginPlacing(PlacementInformation info);
        void Render();
        void Clear();
        void ToggleEraser();
        void Rotate();

        void Update(Vector2 mouseScreen, IMapManager currentMap);
        void HandleNetMessage(NetIncomingMessage msg);
    }
}