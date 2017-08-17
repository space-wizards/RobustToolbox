using Lidgren.Network;
using SFML.System;
using SS14.Shared.Interfaces.Map;
using SS14.Shared;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.Interfaces.Placement
{
    public interface IPlacementManager
    {
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

        void Update(Vector2i mouseScreen, IMapManager currentMap);
        void HandleNetMessage(NetIncomingMessage msg);
    }
}
