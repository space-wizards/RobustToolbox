using System.Collections.Generic;
using JetBrains.Annotations;
using SS14.Client.Graphics.Overlays;

namespace SS14.Client.Interfaces.Graphics.Overlays
{
    [PublicAPI]
    public interface IOverlayManager
    {
        void AddOverlay(Overlay overlay);
        void RemoveOverlay(string id);
        bool HasOverlay(string id);

        Overlay GetOverlay(string id);
        T GetOverlay<T>(string id) where T : Overlay;

        bool TryGetOverlay(string id, out Overlay overlay);
        bool TryGetOverlay<T>(string id, out T overlay) where T : Overlay;

        IEnumerable<Overlay> AllOverlays { get; }
    }

    internal interface IOverlayManagerInternal : IOverlayManager
    {
        void Initialize();
        void FrameUpdate(RenderFrameEventArgs args);
    }
}
