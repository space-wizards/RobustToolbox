using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Graphics;

internal sealed class OverlayManager : IOverlayManagerInternal, IPostInjectInit
{
    [Dependency] private readonly ILogManager _logMan = default!;

    [ViewVariables]
    private readonly Dictionary<Type, HashSet<Overlay>> _overlaysByTypeLookup = new Dictionary<Type, HashSet<Overlay>>();
    private readonly HashSet<Overlay> _overlays = new HashSet<Overlay>();
    private ISawmill _logger = default!;

    public IEnumerable<Overlay> AllOverlays => _overlays;

    public void FrameUpdate(FrameEventArgs args)
    {
        foreach (var overlay in _overlays)
        {
            overlay.FrameUpdate(args);
        }
    }

    public bool AddOverlay(Overlay overlay)
    {
        if (!_overlays.Add(overlay))
            return false;
        var type = overlay.GetType();
        if (!_overlaysByTypeLookup.TryGetValue(type, out var typeSet))
        {
            typeSet = new HashSet<Overlay>();
        }
        typeSet.Add(overlay);
        return true;
    }

    public bool RemoveOverlay(Type overlayClass)
    {
        if (!overlayClass.IsSubclassOf(typeof(Overlay)))
        {
            _logger.Error($"RemoveOverlay was called with arg: {overlayClass}, which is not a subclass of Overlay!");
            return false;
        }

        if (!_overlaysByTypeLookup.TryGetValue(overlayClass, out var typeSet))
            return false;
        foreach (var overlay in typeSet)
        {
            _overlays.Remove(overlay);
        }
        typeSet.Clear();
        return true;
    }

    public bool RemoveOverlay<T>() where T : Overlay
    {
        return RemoveOverlay(typeof(T));
    }

    public bool RemoveOverlay(Overlay overlay)
    {
        if (!_overlays.Remove(overlay))
            return false;
        _overlaysByTypeLookup.GetValueOrDefault(overlay.GetType())?.Remove(overlay);
        return true;
    }

    public bool TryGetOverlay(Type overlayClass, [NotNullWhen(true)] out Overlay? overlay)
    {
        overlay = null;
        if (!overlayClass.IsSubclassOf(typeof(Overlay)))
        {
            _logger.Error($"TryGetOverlay was called with arg: {overlayClass}, which is not a subclass of Overlay!");
            return false;
        }

        if (!_overlaysByTypeLookup.TryGetValue(overlayClass, out var typeSet))
            return false;
        overlay = typeSet.FirstOrDefault();
        return overlay != null;
    }

    public bool TryGetOverlay<T>([NotNullWhen(true)] out T? overlay) where T : Overlay
    {
        overlay = null;
        if (!_overlaysByTypeLookup.TryGetValue(typeof(T), out var typeSet))
            return false;
        overlay = typeSet.FirstOrDefault() as T;
        return overlay != null;
    }

    public Overlay GetOverlay(Type overlayClass)
    {
        return _overlaysByTypeLookup.GetValueOrDefault(overlayClass)?.FirstOrDefault() ?? throw new KeyNotFoundException($"Overlay of type {overlayClass} is not found");
    }

    public T GetOverlay<T>() where T : Overlay
    {
        return (T)GetOverlay(typeof(T));
    }

    public bool HasOverlay(Type overlayClass)
    {
        if (!overlayClass.IsSubclassOf(typeof(Overlay)))
        {
            _logger.Error($"HasOverlay was called with arg: {overlayClass}, which is not a subclass of Overlay!");
        }

        return _overlaysByTypeLookup.GetValueOrDefault(overlayClass)?.Count > 0;
    }

    public bool HasOverlay<T>() where T : Overlay
    {
        return _overlaysByTypeLookup.GetValueOrDefault(typeof(T))?.Count > 0;
    }

    public bool HasOverlay(Overlay overlay)
    {
        return _overlays.Contains(overlay);
    }

    void IPostInjectInit.PostInject()
    {
        _logger = _logMan.GetSawmill("overlay");
    }
}
