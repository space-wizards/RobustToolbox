using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if GODOT
using VS = Godot.VisualServer;

#endif

namespace SS14.Client.Graphics.Overlays
{
    internal class OverlayManager : IOverlayManager
    {
#if GODOT
        private Godot.Node2D RootNodeWorld;
        private Godot.Node2D RootNodeScreen;

        [Dependency] readonly ISceneTreeHolder sceneTreeHolder;
#endif

#if GODOT
        private readonly Dictionary<string, (IOverlay overlay, Godot.RID canvasItem)> overlays =
            new Dictionary<string, (IOverlay, Godot.RID)>();
#endif

        public void Initialize()
        {
#if GODOT
            RootNodeWorld = new Godot.Node2D {Name = "OverlayRoot"};
            sceneTreeHolder.WorldRoot.AddChild(RootNodeWorld);
            RootNodeWorld.ZIndex = (int) DrawDepth.Overlays;

            RootNodeScreen = new Godot.Node2D {Name = "OverlayRoot"};
            sceneTreeHolder.SceneTree.Root.GetNode("UILayer").AddChild(RootNodeScreen);
#endif
        }

        public void FrameUpdate(RenderFrameEventArgs args)
        {
#if GODOT
            foreach (var (overlay, _) in overlays.Values)
            {
                overlay.FrameUpdate(args);
            }
#endif
        }

        public void AddOverlay(IOverlay overlay)
        {
#if GODOT
            if (overlays.ContainsKey(overlay.ID))
            {
                throw new InvalidOperationException($"We already have an overlay with ID '{overlay.ID}'");
            }

            Godot.RID parent;
            switch (overlay.Space)
            {
                case OverlaySpace.ScreenSpace:
                    parent = RootNodeScreen.GetCanvasItem();
                    break;
                case OverlaySpace.WorldSpace:
                    parent = RootNodeWorld.GetCanvasItem();
                    break;
                default:
                    throw new NotImplementedException($"Unknown overlay space: {overlay.Space}");
            }

            var item = VS.CanvasItemCreate();
            VS.CanvasItemSetParent(item, parent);

            overlays.Add(overlay.ID, (overlay, item));
            overlay.AssignCanvasItem(item);
#endif
        }

        public IOverlay GetOverlay(string id)
        {
#if GODOT
            return overlays[id].overlay;
#else
            throw new NotImplementedException();
#endif
        }

        public T GetOverlay<T>(string id) where T : IOverlay
        {
            return (T) GetOverlay(id);
        }

        public bool HasOverlay(string id)
        {
#if GODOT
            return overlays.ContainsKey(id);
#else
            throw new NotImplementedException();
#endif
        }

        public void RemoveOverlay(string id)
        {
#if GODOT
            if (!overlays.TryGetValue(id, out var value))
            {
                return;
            }

            var (overlay, item) = value;
            overlay.Dispose();
            VS.FreeRid(item);
            overlays.Remove(id);
#endif
        }

        public bool TryGetOverlay(string id, out IOverlay overlay)
        {
#if GODOT
            if (overlays.TryGetValue(id, out var value))
            {
                overlay = value.overlay;
                return true;
            }

            overlay = null;
            return false;
#else
            overlay = default;
            return false;
#endif
        }

        public bool TryGetOverlay<T>(string id, out T overlay) where T : IOverlay
        {
#if GODOT
            if (overlays.TryGetValue(id, out var value))
            {
                overlay = (T) value.overlay;
                return true;
            }
#endif
            overlay = default;
            return false;
        }
    }
}
