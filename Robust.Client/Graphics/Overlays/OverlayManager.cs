using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.Utility;
using VS = Godot.VisualServer;

namespace Robust.Client.Graphics.Overlays
{
    internal class OverlayManager : IOverlayManagerInternal
    {
        private Godot.Node2D RootNodeWorld;
        private Godot.Node2D RootNodeScreen;
        private Godot.Node2D RootNodeScreenBelowWorld;

        [Dependency] readonly ISceneTreeHolder sceneTreeHolder;

        private readonly Dictionary<string, Overlay> _overlays = new Dictionary<string, Overlay>();
        private readonly Dictionary<Overlay, Godot.RID> _canvasItems = new Dictionary<Overlay, Godot.RID>();

        public void Initialize()
        {
            if (!GameController.OnGodot)
            {
                return;
            }

            DebugTools.AssertNull(RootNodeScreenBelowWorld);

            RootNodeScreenBelowWorld = new Godot.Node2D { Name = "OverlayRoot" };
            sceneTreeHolder.BelowWorldScreenSpace.AddChild(RootNodeScreenBelowWorld);

            RootNodeWorld = new Godot.Node2D { Name = "OverlayRoot" };
            sceneTreeHolder.WorldRoot.AddChild(RootNodeWorld);
            RootNodeWorld.ZIndex = (int) DrawDepth.Overlays;

            RootNodeScreen = new Godot.Node2D {Name = "OverlayRoot"};
            sceneTreeHolder.SceneTree.Root.GetNode("UILayer").AddChild(RootNodeScreen);
        }

        public void FrameUpdate(RenderFrameEventArgs args)
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
            if (GameController.OnGodot)
            {
                Godot.RID parent;
                switch (overlay.Space)
                {
                    case OverlaySpace.ScreenSpace:
                        parent = RootNodeScreen.GetCanvasItem();
                        break;
                    case OverlaySpace.WorldSpace:
                        parent = RootNodeWorld.GetCanvasItem();
                        break;
                    case OverlaySpace.ScreenSpaceBelowWorld:
                        parent = RootNodeScreenBelowWorld.GetCanvasItem();
                        break;
                    default:
                        throw new NotImplementedException($"Unknown overlay space: {overlay.Space}");
                }

                var item = VS.CanvasItemCreate();
                VS.CanvasItemSetParent(item, parent);
                overlay.AssignCanvasItem(item);
                _canvasItems.Add(overlay, item);
            }
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

            if (GameController.OnGodot)
            {
                var item = _canvasItems[overlay];
                _canvasItems.Remove(overlay);
                VS.FreeRid(item);
            }
        }

        public bool TryGetOverlay(string id, out Overlay overlay)
        {
            return _overlays.TryGetValue(id, out overlay);
        }

        public bool TryGetOverlay<T>(string id, out T overlay) where T : Overlay
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
