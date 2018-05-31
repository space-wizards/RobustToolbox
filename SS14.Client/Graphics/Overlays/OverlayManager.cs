using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VS = Godot.VisualServer;

namespace SS14.Client.Graphics.Overlays
{
    internal class OverlayManager : IOverlayManager
    {
        private Godot.Node2D RootNodeWorld;
        private Godot.Node2D RootNodeScreen;

        [Dependency]
        readonly ISceneTreeHolder sceneTreeHolder;

        private readonly Dictionary<string, (IOverlay overlay, Godot.RID canvasItem)> overlays = new Dictionary<string, (IOverlay, Godot.RID)>();

        public void Initialize()
        {
            RootNodeWorld = new Godot.Node2D { Name = "OverlayRoot" };
            sceneTreeHolder.WorldRoot.AddChild(RootNodeWorld);

            RootNodeScreen = new Godot.Node2D { Name = "OverlayRoot" };
            sceneTreeHolder.SceneTree.Root.GetNode("UILayer").AddChild(RootNodeScreen);
        }

        public void FrameUpdate(RenderFrameEventArgs args)
        {
            foreach (var (overlay, _) in overlays.Values)
            {
                overlay.FrameUpdate(args);
            }
        }

        public void AddOverlay(IOverlay overlay)
        {
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
        }

        public IOverlay GetOverlay(string id)
        {
            return overlays[id].overlay;
        }

        public T GetOverlay<T>(string id) where T : IOverlay
        {
            return (T)GetOverlay(id);
        }

        public bool HasOverlay(string id)
        {
            return overlays.ContainsKey(id);
        }

        public void RemoveOverlay(string id)
        {
            if (!overlays.TryGetValue(id, out var value))
            {
                return;
            }
            var (overlay, item) = value;
            overlay.ClearCanvasItem();
            VS.FreeRid(item);
            overlays.Remove(id);
        }

        public bool TryGetOverlay(string id, out IOverlay overlay)
        {
            if (overlays.TryGetValue(id, out var value))
            {
                overlay = value.overlay;
                return true;
            }
            overlay = null;
            return false;
        }

        public bool TryGetOverlay<T>(string id, out T overlay) where T : IOverlay
        {
            if (overlays.TryGetValue(id, out var value))
            {
                overlay = (T)value.overlay;
                return true;
            }
            overlay = default(T);
            return false;
        }
    }
}
