using System;
using System.Linq;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Lighting
{
    partial class LightManager
    {
        sealed class Occluder : IOccluder
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

            public bool Disposed { get; private set; }

            private LightManager Manager;

            private Godot.OccluderPolygon2D occluderPolygon;
            private Godot.LightOccluder2D occluder;

            private bool Deferred => Manager.System == LightingSystem.Deferred;

            public OccluderCullMode CullMode
            {
                get => GameController.OnGodot ? (OccluderCullMode) occluderPolygon.CullMode : default;
                set
                {
                    if (GameController.OnGodot)
                    {
                        occluderPolygon.CullMode = (Godot.OccluderPolygon2D.CullModeEnum) value;
                    }
                }
            }


            private IGodotTransformComponent parentTransform;
            private Godot.Vector2 CurrentPos;

            public Occluder(LightManager manager)
            {
                Manager = manager;

                if (!GameController.OnGodot)
                {
                    return;
                }

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
                if (Disposed)
                {
                    return;
                }

                Manager.RemoveOccluder(this);
                Disposed = true;

                if (!GameController.OnGodot)
                {
                    return;
                }

                occluder.QueueFree();
                occluder.Dispose();

                occluderPolygon.Dispose();
            }

            public void SetPolygon(Vector2[] polygon)
            {
                if (!GameController.OnGodot)
                {
                    return;
                }

                var converted = new Godot.Vector2[polygon.Length];
                for (var i = 0; i < polygon.Length; i++)
                {
                    converted[i] = polygon[i].Convert();
                }

                occluderPolygon.Polygon = converted;
            }

            public void DeParent()
            {
                if (!GameController.OnGodot)
                {
                    return;
                }

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

            public void ParentTo(ITransformComponent node)
            {
                if (!GameController.OnGodot)
                {
                    return;
                }

                if (!Deferred)
                {
                    ((IGodotTransformComponent) node).SceneNode.AddChild(occluder);
                }

                parentTransform = (IGodotTransformComponent) node;
                UpdateEnabled();
            }


            private void UpdateEnabled()
            {
                if (GameController.OnGodot)
                {
                    occluder.Visible = parentTransform != null && Enabled;
                }
            }

            public void FrameProcess(FrameEventArgs args)
            {
                if (!GameController.OnGodot)
                {
                    return;
                }

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
