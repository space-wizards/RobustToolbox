using SS14.Client.GameObjects;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Overlays;
using SS14.Client.Interfaces.Debugging;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;

namespace SS14.Client.Debugging
{
    public class DebugDrawing : IDebugDrawing
    {
        [Dependency]
        readonly IOverlayManager overlayManager;

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
                    overlayManager.AddOverlay(new CollidableOverlay());
                }
                else
                {
                    overlayManager.RemoveOverlay(nameof(CollidableOverlay));
                }
            }
        }

        private class CollidableOverlay : Overlay
        {
            [Dependency]
            readonly IComponentManager componentManager;
            [Dependency]
            readonly IEyeManager eyeManager;

            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public CollidableOverlay() : base(nameof(CollidableOverlay))
            {
                IoCManager.InjectDependencies(this);
            }

            protected override void Draw(DrawingHandle handle)
            {
                var viewport = eyeManager.GetWorldViewport();
                foreach (var boundingBox in componentManager.GetComponents<BoundingBoxComponent>())
                {
                    // all entities have a TransformComponent
                    var transform = boundingBox.Owner.GetComponent<ITransformComponent>();

                    // if not on the same map, continue
                    if (transform.MapID != eyeManager.CurrentMap)
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

                    const int ppm = EyeManager.PIXELSPERMETER;
                    var screenBox = new Box2(worldBox.TopLeft * ppm, worldBox.BottomRight * ppm);

                    handle.DrawRect(screenBox, colorFill);
                    handle.DrawRect(screenBox, colorEdge, filled: false);
                }
            }
        }
    }
}
