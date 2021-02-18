using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client.Graphics
{
    internal class OverlayManager : IOverlayManagerInternal
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

        public bool RemoveOverlay<T>(){
            return RemoveOverlay(typeof(T));
        }

        public bool RemoveOverlay(Overlay overlay) {
            return _overlays.Remove(overlay.GetType());
        }





        public bool TryGetOverlay(Type overlayClass, out Overlay overlay)
        {
            overlay = null;
            if (!overlayClass.IsSubclassOf(typeof(Overlay))){
                Logger.Error("GetOverlay was called with arg: " + overlayClass.ToString() + ", which is not a subclass of Overlay!");
                return false;
            }
            return _overlays.TryGetValue(overlayClass, out overlay);
        }

        public bool TryGetOverlay<T>(out Overlay overlay){
            return TryGetOverlay(typeof(T), out overlay);
        }



        public bool HasOverlay(Type overlayClass) {
            if (!overlayClass.IsSubclassOf(typeof(Overlay)))
                Logger.Error("HasOverlay was called with arg: " + overlayClass.ToString() + ", which is not a subclass of Overlay!");
            return _overlays.Remove(overlayClass);
        }

        public bool HasOverlay<T>() {
            if (!typeof(T).IsSubclassOf(typeof(Overlay)))
                Logger.Error("HasOverlay was called with arg: " + typeof(T).ToString() + ", which is not a subclass of Overlay!");
            return _overlays.Remove(typeof(T));
        }
    }
}
