using System;
using System.Linq;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Lighting
{
    partial class LightManager
    {
        class Occluder : IOccluder
        {
            private bool visible = true;
            public bool Enabled
            {
                get => visible;
                set
                {
                    visible = value;
                    UpdateEnabled();
                }
            }

            private LightManager Manager;

            private Godot.OccluderPolygon2D occluderPolygon;
            private Godot.LightOccluder2D occluder;

            private bool Deferred => Manager.Deferred;
            private IGodotTransformComponent parentTransform;
            private Godot.Vector2 CurrentPos;

            public Occluder(LightManager manager)
            {
                Manager = manager;

                occluderPolygon = new Godot.OccluderPolygon2D();
                occluder = new Godot.LightOccluder2D()
                {
                    Occluder = occluderPolygon,
                };

                if (Deferred)
                {
                    Manager.deferredViewport.AddChild(occluder);
                }
            }

            public void Dispose()
            {
                // Already disposed.
                if (occluder == null)
                {
                    return;
                }

                Manager.RemoveOccluder(this);
                Manager = null;

                occluder.QueueFree();
                occluder.Dispose();
                occluder = null;

                occluderPolygon.Dispose();
                occluderPolygon = null;
            }

            public void SetPolygon(Vector2[] polygon)
            {
                var converted = new Godot.Vector2[polygon.Length];
                for (var i = 0; i < polygon.Length; i++)
                {
                    converted[i] = polygon[i].Convert();
                }
                occluderPolygon.Polygon = converted;
            }

            public void SetGodotPolygon(Godot.Vector2[] polygon)
            {
                occluderPolygon.Polygon = polygon;
            }

            public void DeParent()
            {
                if (Deferred)
                {
                    occluder.Position = new Godot.Vector2(0, 0);
                }
                else
                {
                    parentTransform.SceneNode.RemoveChild(occluder);
                }
                UpdateEnabled();
            }

            public void ParentTo(IGodotTransformComponent node)
            {
                if (!Deferred)
                {
                    node.SceneNode.AddChild(occluder);
                }
                parentTransform = node;
                UpdateEnabled();
            }

            private void UpdateEnabled()
            {
                occluder.Visible = parentTransform != null && Enabled;
            }

            public void FrameProcess(FrameEventArgs args)
            {
                // TODO: Maybe use OnMove events to make this less expensive.
                if (Deferred && parentTransform != null)
                {
                    var newPos = parentTransform.SceneNode.GlobalPosition;
                    if (CurrentPos != newPos)
                    {
                        occluder.Position = newPos;
                    }
                }
            }
        }
    }
}
