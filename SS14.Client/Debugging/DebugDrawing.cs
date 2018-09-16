using SS14.Client.GameObjects;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Overlays;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.Debugging;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.BoundingBox;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;

namespace SS14.Client.Debugging
{
    public class DebugDrawing : IDebugDrawing
    {
        [Dependency] readonly IOverlayManager _overlayManager;

        private bool _debugColliders = false;

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
                    _overlayManager.AddOverlay(new CollidableOverlay());
                }
                else
                {
                    _overlayManager.RemoveOverlay(nameof(CollidableOverlay));
                }
            }
        }

        private class CollidableOverlay : Overlay
        {
            [Dependency] private readonly IComponentManager _componentManager;
            [Dependency] private readonly IEyeManager _eyeManager;
            [Dependency] private readonly IPrototypeManager _prototypeManager;

            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public CollidableOverlay() : base(nameof(CollidableOverlay))
            {
                IoCManager.InjectDependencies(this);
                Shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();
            }

            protected override void Draw(DrawingHandle handle)
            {
                var worldHandle = (DrawingHandleWorld) handle;
                var viewport = _eyeManager.GetWorldViewport();
                foreach (var boundingBox in _componentManager.GetAllComponents<ClientBoundingBoxComponent>())
                {
                    // all entities have a TransformComponent
                    var transform = boundingBox.Owner.GetComponent<ITransformComponent>();

                    // if not on the same map, continue
                    if (transform.MapID != _eyeManager.CurrentMap)
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
