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
        bool AddOverlay(Overlay overlay);

        bool RemoveOverlay(Overlay overlay);
        bool RemoveOverlay(Type overlayClass);
        bool RemoveOverlay<T>() where T : Overlay;

        bool TryGetOverlay(Type overlayClass, out Overlay overlay);
        bool TryGetOverlay<T>(out T overlay) where T : Overlay;

        Overlay GetOverlay(Type overlayClass);
        T GetOverlay<T>() where T : Overlay;

        bool HasOverlay(Type overlayClass);
        bool HasOverlay<T>() where T : Overlay;

        IEnumerable<Overlay> AllOverlays { get; }
    }

    internal interface IOverlayManagerInternal : IOverlayManager
    {
        void FrameUpdate(FrameEventArgs args);
    }
}
