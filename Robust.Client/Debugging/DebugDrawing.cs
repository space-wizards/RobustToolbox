using Robust.Client.GameObjects;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Debugging;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Robust.Client.Debugging
{
    public class DebugDrawing : IDebugDrawing
    {
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IComponentManager _componentManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;

        private bool _debugColliders;

        public bool DebugColliders
        {
            get => _debugColliders;
            set
            {
                if (value == DebugColliders)
                {
                    return;
                }

                _debugColliders = value;

                if (value)
                {
                    _overlayManager.AddOverlay(new CollidableOverlay(_componentManager, _eyeManager, _prototypeManager));
                }
                else
                {
                    _overlayManager.RemoveOverlay(nameof(CollidableOverlay));
                }
            }
        }

        private class CollidableOverlay : Overlay
        {
            private readonly IComponentManager _componentManager;
            private readonly IEyeManager _eyeManager;
            private readonly IPrototypeManager _prototypeManager;

            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public CollidableOverlay(IComponentManager compMan, IEyeManager eyeMan, IPrototypeManager protoMan)
                : base(nameof(CollidableOverlay))
            {
                _componentManager = compMan;
                _eyeManager = eyeMan;
                _prototypeManager = protoMan;

                Shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();
            }

            protected override void Draw(DrawingHandle handle)
            {
                var worldHandle = (DrawingHandleWorld) handle;
                var viewport = _eyeManager.GetWorldViewport();
                foreach (var boundingBox in _componentManager.GetAllComponents<ClientBoundingBoxComponent>())
                {
                    // all entities have a TransformComponent
                    var transform = boundingBox.Owner.Transform;

                    // if not on the same map, continue
                    if (transform.MapID != _eyeManager.CurrentMap || !transform.IsMapTransform)
                        continue;

                    var colorEdge = boundingBox.DebugColor.WithAlpha(0.33f);
                    var colorFill = boundingBox.DebugColor.WithAlpha(0.25f);
                    Box2 worldBox;
                    if (boundingBox.Owner.TryGetComponent<ICollidableComponent>(out var collision))
                    {
                        worldBox = collision.WorldAABB;
                    }
                    else
                    {
                        worldBox = boundingBox.WorldAABB;
                    }

                    // if not on screen, or too small, continue
                    if (!worldBox.Intersects(viewport) || worldBox.IsEmpty())
                        continue;

                    worldHandle.DrawRect(worldBox, colorFill);
                    worldHandle.DrawRect(worldBox, colorEdge, filled: false);
                }
            }
        }
    }
}
