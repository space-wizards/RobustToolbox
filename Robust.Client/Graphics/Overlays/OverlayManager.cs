using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.Graphics.Interfaces.Graphics.Overlays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Overlays
{
    internal class OverlayManager : IOverlayManagerInternal
    {
        private readonly Dictionary<string, Overlay> _overlays = new();

        public void FrameUpdate(FrameEventArgs args)
        {
            foreach (var overlay in _overlays.Values)
            {
                overlay.FrameUpdate(args);
            }
        }

        public void AddOverlay(Overlay overlay)
        {
            if (_overlays.ContainsKey(overlay.ID))
            {
                throw new InvalidOperationException($"We already have an overlay with ID '{overlay.ID}'");
            }

            _overlays.Add(overlay.ID, overlay);
        }

        public Overlay GetOverlay(string id)
        {
            return _overlays[id];
        }

        public T GetOverlay<T>(string id) where T : Overlay
        {
            return (T) GetOverlay(id);
        }

        public bool HasOverlay(string id)
        {
            return _overlays.ContainsKey(id);
        }

        public void RemoveOverlay(string id)
        {
            if (!_overlays.TryGetValue(id, out var overlay))
            {
                return;
            }

            overlay.Dispose();
            _overlays.Remove(id);
        }

        public bool TryGetOverlay(string id, [NotNullWhen(true)] out Overlay? overlay)
        {
            return _overlays.TryGetValue(id, out overlay);
        }

        public bool TryGetOverlay<T>(string id, [NotNullWhen(true)] out T? overlay) where T : Overlay
        {
            if (_overlays.TryGetValue(id, out var value))
            {
                overlay = (T) value;
                return true;
            }

            overlay = default;
            return false;
        }

        public IEnumerable<Overlay> AllOverlays => _overlays.Values;
    }
}
