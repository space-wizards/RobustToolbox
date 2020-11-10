using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Client.Graphics.Overlays;
using Robust.Shared.Timing;

namespace Robust.Client.Interfaces.Graphics.Overlays
{
    [PublicAPI]
    public interface IOverlayManager
    {
        void AddOverlay(Guid id, Overlay overlay);
        void RemoveOverlay(Guid id);
        void RemoveOverlaysOfClass(string className);
        bool HasOverlay(Guid id);
        bool HasOverlayOfClass(string className);
        bool HasOverlayOfType<T>();

        Overlay GetOverlay(Guid id);
        bool GetOverlaysOfClass<T>(out List<T> overlays) where T : Overlay;
        bool GetOverlaysOfClass(string className, out List<Overlay> overlays);
        int GetOverlayTypeCount<T>() where T : Overlay;

        bool TryGetOverlay(Guid id, [NotNullWhen(true)] out Overlay? overlay);
        bool TryGetOverlay<T>(Guid id, [NotNullWhen(true)] out T? overlay) where T : Overlay;

        IEnumerable<Overlay> AllOverlays { get; }
    }

    internal interface IOverlayManagerInternal : IOverlayManager
    {
        void FrameUpdate(FrameEventArgs args);
    }
}
