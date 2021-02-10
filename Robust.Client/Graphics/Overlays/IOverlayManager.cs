using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Robust.Client.Graphics
{
    [PublicAPI]
    public interface IOverlayManager
    {
        void AddOverlay(Overlay overlay);
        void RemoveOverlay(string id);
        bool HasOverlay(string id);

        Overlay GetOverlay(string id);
        T GetOverlay<T>(string id) where T : Overlay;

        bool TryGetOverlay(string id, [NotNullWhen(true)] out Overlay? overlay);
        bool TryGetOverlay<T>(string id, [NotNullWhen(true)] out T? overlay) where T : Overlay;

        IEnumerable<Overlay> AllOverlays { get; }
    }

    internal interface IOverlayManagerInternal : IOverlayManager
    {
        void FrameUpdate(FrameEventArgs args);
    }
}
