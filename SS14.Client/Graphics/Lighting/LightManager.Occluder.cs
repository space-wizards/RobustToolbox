using System;
using System.Linq;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Utility;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Lighting
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

            private LightManager Manager;

            #if GODOT
            private Godot.OccluderPolygon2D occluderPolygon;
            private Godot.LightOccluder2D occluder;
            #endif

            private bool Deferred => Manager.System == LightingSystem.Deferred;

            public OccluderCullMode CullMode
            {
                #if GODOT
                get => (OccluderCullMode)occluderPolygon.CullMode;
                set => occluderPolygon.CullMode = (Godot.OccluderPolygon2D.CullModeEnum)value;
                #else
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
                #endif
            }

            #if GODOT
            private IGodotTransformComponent parentTransform;
            private Godot.Vector2 CurrentPos;
            #endif

            public Occluder(LightManager manager)
            {
                Manager = manager;

                #if GODOT
                occluderPolygon = new Godot.OccluderPolygon2D();
                occluder = new Godot.LightOccluder2D()
                {
                    Occluder = occluderPolygon,
                };

                if (Deferred)
                {
                    Manager.deferredViewport.AddChild(occluder);
                }
                #endif
            }

            public void Dispose()
            {
                #if GODOT
                // Already disposed.
                if (occluder == null)
                {
                    return;
                }
                #endif

                Manager.RemoveOccluder(this);
                Manager = null;

                #if GODOT
                occluder.QueueFree();
                occluder.Dispose();
                occluder = null;

                occluderPolygon.Dispose();
                occluderPolygon = null;
                #endif
            }

            public void SetPolygon(Vector2[] polygon)
            {
                #if GODOT
                var converted = new Godot.Vector2[polygon.Length];
                for (var i = 0; i < polygon.Length; i++)
                {
                    converted[i] = polygon[i].Convert();
                }
                occluderPolygon.Polygon = converted;
                #endif
            }

            public void DeParent()
            {
                #if GODOT
                if (Deferred)
                {
                    occluder.Position = new Godot.Vector2(0, 0);
                }
                else
                {
                    parentTransform.SceneNode.RemoveChild(occluder);
                }
                #endif
                UpdateEnabled();
            }

            public void ParentTo(ITransformComponent node)
            {
#if GODOT
                if (!Deferred)
                {
                    node.SceneNode.AddChild(occluder);
                }
                parentTransform = (IGodotTransformComponent)node;
                UpdateEnabled();
#endif
            }


            private void UpdateEnabled()
            {
                #if GODOT
                occluder.Visible = parentTransform != null && Enabled;
                #endif
            }

            public void FrameProcess(FrameEventArgs args)
            {
                #if GODOT
                // TODO: Maybe use OnMove events to make this less expensive.
                if (Deferred && parentTransform != null)
                {
                    var newPos = parentTransform.SceneNode.GlobalPosition;
                    if (CurrentPos != newPos)
                    {
                        occluder.Position = newPos;
                    }
                }
                #endif
            }
        }
    }
}
