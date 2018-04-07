using SS14.Client.Graphics.ClientEye;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Utility;

namespace SS14.Client.GameObjects
{
    /// <summary>
    ///     Supports the debug drawing of colliders.
    /// </summary>
    public class GodotCollidableComponent : CollidableComponent
    {
        public override bool DebugDraw
        {
            set
            {
                if (value == _debugDraw)
                {
                    return;
                }

                _debugDraw = value;
                debugNode.Update();
            }
        }

        private Godot.Node2D debugNode;
        private IGodotTransformComponent transform;
        private GodotGlue.GodotSignalSubscriber0 debugDrawSubscriber;

        public override void Initialize()
        {
            transform = Owner.GetComponent<IGodotTransformComponent>();
            debugNode = new Godot.Node2D();
            debugNode.SetName("Collidable debug");
            debugDrawSubscriber = new GodotGlue.GodotSignalSubscriber0();
            debugDrawSubscriber.Connect(debugNode, "draw");
            debugDrawSubscriber.Signal += DrawDebugRect;
            transform.SceneNode.AddChild(debugNode);

            base.Initialize();
        }

        public override void OnRemove()
        {
            base.OnRemove();

            debugDrawSubscriber.Disconnect(debugNode, "draw");
            debugDrawSubscriber.Dispose();
            debugDrawSubscriber = null;

            debugNode.QueueFree();
            debugNode.Dispose();
            debugNode = null;
        }

        private void DrawDebugRect()
        {
            if (!DebugDraw)
            {
                return;
            }
            var colorEdge = DebugColor.WithAlpha(0.50f).Convert();
            var colorFill = DebugColor.WithAlpha(0.25f).Convert();
            var aabb = Owner.GetComponent<BoundingBoxComponent>().AABB;

            const int ppm = EyeManager.PIXELSPERMETER;
            var rect = new Godot.Rect2(aabb.Left * ppm, aabb.Top * ppm, aabb.Width * ppm, aabb.Height * ppm);
            debugNode.DrawRect(rect, colorEdge, filled: false);
            rect.Position += new Godot.Vector2(1, 1);
            rect.Size -= new Godot.Vector2(2, 2);
            debugNode.DrawRect(rect, colorFill, filled: true);
        }
    }
}
