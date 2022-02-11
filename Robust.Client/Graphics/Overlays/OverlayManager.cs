using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client.Graphics
{
    internal sealed class OverlayManager : IOverlayManagerInternal
    {
        private readonly Dictionary<Type, Overlay> _overlays = new Dictionary<Type, Overlay>();
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
            if(_overlays.ContainsKey(overlay.GetType()))
                return false;
            _overlays.Add(overlay.GetType(), overlay);
            return true;
        }



        public bool RemoveOverlay(Type overlayClass)
        {
            if(!overlayClass.IsSubclassOf(typeof(Overlay))){
                Logger.Error("RemoveOverlay was called with arg: " + overlayClass.ToString() + ", which is not a subclass of Overlay!");
                return false;
            }
            return _overlays.Remove(overlayClass);
        }

        public bool RemoveOverlay<T>() where T : Overlay{
            return RemoveOverlay(typeof(T));
        }

        public bool RemoveOverlay(Overlay overlay) {
            return _overlays.Remove(overlay.GetType());
        }





        public bool TryGetOverlay(Type overlayClass, [NotNullWhen(true)] out Overlay? overlay)
        {
            overlay = null;
            if (!overlayClass.IsSubclassOf(typeof(Overlay))){
                Logger.Error("TryGetOverlay was called with arg: " + overlayClass.ToString() + ", which is not a subclass of Overlay!");
                return false;
            }
            return _overlays.TryGetValue(overlayClass, out overlay);
        }

        public bool TryGetOverlay<T>([NotNullWhen(true)] out T? overlay) where T : Overlay {
            overlay = null;
            if(_overlays.TryGetValue(typeof(T), out Overlay? toReturn)){
                overlay = (T)toReturn;
                return true;
            }
            return false;
        }




        public Overlay GetOverlay(Type overlayClass) {
            return _overlays[overlayClass];
        }

        public T GetOverlay<T>() where T : Overlay {
            return (T)_overlays[typeof(T)];
        }





        public bool HasOverlay(Type overlayClass) {
            if (!overlayClass.IsSubclassOf(typeof(Overlay)))
                Logger.Error("HasOverlay was called with arg: " + overlayClass.ToString() + ", which is not a subclass of Overlay!");
            return _overlays.ContainsKey(overlayClass);
        }

        public bool HasOverlay<T>() where T : Overlay  {
            return _overlays.ContainsKey(typeof(T));
        }

    }
}
