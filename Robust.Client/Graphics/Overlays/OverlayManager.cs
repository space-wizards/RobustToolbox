using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Timing;

namespace Robust.Client.Graphics
{
    internal class OverlayManager : IOverlayManagerInternal
    {
        private readonly Dictionary<String, Overlay> _overlays = new Dictionary<String, Overlay>();
        public IEnumerable<Overlay> AllOverlays => _overlays.Values;

        public void FrameUpdate(FrameEventArgs args)
        {
            foreach (var overlay in _overlays.Values)
            {
                overlay.FrameUpdate(args);
            }
        }

        public void AddOverlay(string id, Overlay overlay) {
            if (_overlays.ContainsKey(id)) {
                throw new InvalidOperationException($"We already have an overlay with guid '{id}'!");
            }

            _overlays.Add(id, overlay);
        }
        public void RemoveOverlay(string id) {
            if (!_overlays.TryGetValue(id, out var overlay)) {
                return;
            }
            _overlays.Remove(id);
        }
        public void RemoveOverlaysOfClass(string className) {
            var overlaysCopy = new Dictionary<string, Overlay>(_overlays);
            foreach (var (id, overlay) in overlaysCopy) {
                if (nameof(overlay) == className) {
                    _overlays.Remove(id);
                }
            }

        }
        public bool HasOverlay(string id) {
            return _overlays.ContainsKey(id);
        }
        public bool HasOverlayOfClass(string className) {
            foreach (var overlay in _overlays.Values) {
                if (overlay.GetType().ToString() == className) {
                    return true;
                }
            }
            return false;
        }
        public bool HasOverlayOfType<T>() {
            foreach (var overlay in _overlays.Values) {
                if (overlay.GetType() == typeof(T)) {
                    return true;
                }
            }
            return false;
        }

        public Overlay GetOverlay(string id)
        {
            return _overlays[id];
        }

        public bool TryGetOverlaysOfClass<T>(out List<T> overlays) where T : Overlay
        {
            overlays = new List<T>();
            foreach (var overlay in _overlays.Values) {
                if (overlay.GetType() == typeof(T))
                    overlays.Add((T)overlay);
            }
            return overlays.Count > 0;
        }
        public bool TryGetOverlaysOfClass(string className, out List<Overlay> overlays) {
            Type? type = Type.GetType(className);
            overlays = new List<Overlay>();
            if(type == null)
                throw new InvalidOperationException("Class '" + className + "' was requested in GetOverlaysOfClass, but no such class exists!");
            if(!type.IsSubclassOf(typeof(Overlay)))
                throw new InvalidOperationException("Class '" + className + "' was requested in GetOverlaysOfClass, but this class is not a child of Overlay!");

            if (type != null) {
                foreach (var overlay in _overlays.Values) {
                    if (overlay.GetType() == type)
                        overlays.Add(overlay);
                }
            }
            return overlays.Count > 0;
        }

        public int GetOverlayTypeCount<T>() where T : Overlay
        {
            int i = 0;
            foreach (var overlay in _overlays.Values) {
                if (overlay.GetType() == typeof(T))
                    i++;
            }
            return i;
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
    }

    class OverlayID {
        public int ID;
    }
}
