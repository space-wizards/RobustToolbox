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
        void AddOverlay(Overlay overlay);
        bool RemoveOverlay(Type overlayClass);
        bool TryGetOverlay(Type overlayClass, out Overlay overlay);
        bool HasOverlay(Type overlayClass);

        IEnumerable<Overlay> AllOverlays { get; }
    }

    internal interface IOverlayManagerInternal : IOverlayManager
    {
        void FrameUpdate(FrameEventArgs args);
    }
}
