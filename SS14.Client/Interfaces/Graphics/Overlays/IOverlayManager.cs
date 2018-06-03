using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Interfaces.Graphics.Overlays
{
    public interface IOverlayManager
    {
        void Initialize();
        void FrameUpdate(RenderFrameEventArgs args);

        void AddOverlay(IOverlay overlay);
        void RemoveOverlay(string id);
        bool HasOverlay(string id);

        IOverlay GetOverlay(string id);
        T GetOverlay<T>(string id) where T : IOverlay;

        bool TryGetOverlay(string id, out IOverlay overlay);
        bool TryGetOverlay<T>(string id, out T overlay) where T : IOverlay;
    }
}
