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
        void AddOverlay(Guid ID, Overlay overlay);
        void RemoveOverlay(Guid ID);
        bool HasOverlay(Guid ID);

        Overlay GetOverlay(Guid ID);
        T[] GetOverlaysOfType<T>() where T : Overlay;
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
