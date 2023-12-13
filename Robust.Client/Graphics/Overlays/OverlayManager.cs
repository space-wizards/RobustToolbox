using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Graphics;

internal sealed class OverlayManager : IOverlayManagerInternal, IPostInjectInit
{
    [Dependency] private readonly ILogManager _logMan = default!;

    [ViewVariables]
    private readonly Dictionary<Type, Overlay> _overlays = new Dictionary<Type, Overlay>();
    private ISawmill _logger = default!;

    public IEnumerable<Overlay> AllOverlays => _overlays.Values;

    public void FrameUpdate(FrameEventArgs args)
    {
        foreach (var overlay in _overlays.Values)
        {
            overlay.FrameUpdate(args);
        }
    }

    public bool AddOverlay(Overlay overlay)
    {
        if (_overlays.ContainsKey(overlay.GetType()))
            return false;
        _overlays.Add(overlay.GetType(), overlay);
        return true;
    }

    public bool RemoveOverlay(Type overlayClass)
    {
        if (!overlayClass.IsSubclassOf(typeof(Overlay)))
        {
            _logger.Error($"RemoveOverlay was called with arg: {overlayClass}, which is not a subclass of Overlay!");
            return false;
        }

        return _overlays.Remove(overlayClass);
    }

    public bool RemoveOverlay<T>() where T : Overlay
    {
        return RemoveOverlay(typeof(T));
    }

    public bool RemoveOverlay(Overlay overlay)
    {
        return _overlays.Remove(overlay.GetType());
    }

    public bool TryGetOverlay(Type overlayClass, [NotNullWhen(true)] out Overlay? overlay)
    {
        overlay = null;
        if (!overlayClass.IsSubclassOf(typeof(Overlay)))
        {
            _logger.Error($"TryGetOverlay was called with arg: {overlayClass}, which is not a subclass of Overlay!");
            return false;
        }

        return _overlays.TryGetValue(overlayClass, out overlay);
    }

    public bool TryGetOverlay<T>([NotNullWhen(true)] out T? overlay) where T : Overlay
    {
        overlay = null;
        if (_overlays.TryGetValue(typeof(T), out Overlay? toReturn))
        {
            overlay = (T)toReturn;
            return true;
        }

        return false;
    }

    public Overlay GetOverlay(Type overlayClass)
    {
        return _overlays[overlayClass];
    }

    public T GetOverlay<T>() where T : Overlay
    {
        return (T)_overlays[typeof(T)];
    }

    public bool HasOverlay(Type overlayClass)
    {
        if (!overlayClass.IsSubclassOf(typeof(Overlay)))
        {
            _logger.Error($"HasOverlay was called with arg: {overlayClass}, which is not a subclass of Overlay!");
        }

        return _overlays.ContainsKey(overlayClass);
    }

    public bool HasOverlay<T>() where T : Overlay
    {
        return _overlays.ContainsKey(typeof(T));
    }

    void IPostInjectInit.PostInject()
    {
        _logger = _logMan.GetSawmill("overlay");
    }
}
