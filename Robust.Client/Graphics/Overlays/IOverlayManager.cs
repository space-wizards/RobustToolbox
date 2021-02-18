using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Robust.Client.Graphics
{

    [PublicAPI]
    public interface IOverlayManager
    {
        void AddOverlay(string id, Overlay overlay);
        void RemoveOverlay(string id);
        void RemoveOverlaysOfClass(string className);
        bool HasOverlay(string id);
        bool HasOverlayOfClass(string className);
        bool HasOverlayOfType<T>();

        Overlay GetOverlay(string id);
        bool TryGetOverlaysOfClass<T>(out List<T> overlays) where T : Overlay;
        bool TryGetOverlaysOfClass(string className, out List<Overlay> overlays);
        int GetOverlayTypeCount<T>() where T : Overlay;

        bool TryGetOverlay(string id, [NotNullWhen(true)] out Overlay? overlay);
        bool TryGetOverlay<T>(string id, [NotNullWhen(true)] out T? overlay) where T : Overlay;

        IEnumerable<Overlay> AllOverlays { get; }
    }

    internal interface IOverlayManagerInternal : IOverlayManager
    {
        void FrameUpdate(FrameEventArgs args);
    }
}
