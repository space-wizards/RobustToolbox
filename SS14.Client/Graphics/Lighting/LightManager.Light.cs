using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Utility;
using SS14.Shared;
using SS14.Shared.Maths;
using System;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.GameObjects.Components;

namespace SS14.Client.Graphics.Lighting
{
    partial class LightManager
    {
        sealed class Light : ILight
        {
            public Vector2 Offset
            {
#if GODOT
                get => Light2D.Offset.Convert();
                set => Light2D.Offset = value.Convert();
                #else
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
#endif
            }

            public Angle Rotation
            {
#if GODOT
                get => new Angle(Light2D.GlobalRotation);
                set => Light2D.Rotation = (float)value;
#else
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
#endif
            }

            private Color color;

            public Color Color
            {
                get => color;
                set
                {
                    if (value == color)
                    {
                        return;
                    }

                    color = value;
                    #if GODOT
                    Light2D.Color = value.Convert();
                    #endif
                }
            }

            private float textureScale;

            public float TextureScale
            {
                get => textureScale;
                set
                {
                    if (value == textureScale)
                    {
                        return;
                    }

                    textureScale = value;
                    #if GODOT
                    Light2D.TextureScale = value;
                    #endif
                }
            }

            private float energy;

            public float Energy
            {
                get => energy;
                set
                {
                    if (value == energy)
                    {
                        return;
                    }

                    energy = value;
                    #if GODOT
                    Light2D.Energy = value;
                    #endif
                }
            }

            public ILightMode Mode { get; private set; }

            public LightModeClass ModeClass
            {
                get => Mode.ModeClass;
                set
                {
                    if (value == ModeClass)
                    {
                        return;
                    }

                    // In this order so IF something blows up making the new instance,
                    // this light doesn't corrupt completely and throw exceptions everywhere.
                    var newMode = GetModeInstance(value);
                    Mode.Shutdown();
                    Mode = newMode;
                    Mode.Start(this);
                }
            }

            private Texture texture;

            public Texture Texture
            {
                get => texture;
                set
                {
                    if (texture == value)
                    {
                        return;
                    }

                    texture = value;
                    #if GODOT
                    Light2D.Texture = value;
                    #endif
                }
            }

            private bool enabled;

            public bool Enabled
            {
                get => enabled;
                set
                {
                    enabled = value;
                    UpdateEnabled();
                }
            }

            private LightManager Manager;
            private LightingSystem System => Manager.System;
#if GODOT
            private Godot.Light2D Light2D;

            private IGodotTransformComponent parentTransform;
            private Godot.Vector2 CurrentPos;
#endif
            public Light(LightManager manager)
            {
                Manager = manager;
#if GODOT
                Light2D = new Godot.Light2D()
                {
                    // TODO: Allow this to be modified.
                    ShadowEnabled = true,
                    ShadowFilter = Godot.Light2D.ShadowFilterEnum.Pcf5,
                };

                if (Manager.System == LightingSystem.Disabled)
                {
                    Light2D.Enabled = Light2D.Visible = false;
                }
                #endif

                Mode = new LightModeConstant();
                Mode.Start(this);

#if GODOT
                if (System == LightingSystem.Deferred)
                {
                    Manager.deferredViewport.AddChild(Light2D);
                }
                #endif
            }

            public void DeParent()
            {
#if GODOT
                if (System == LightingSystem.Deferred)
                {
                    Light2D.Position = new Godot.Vector2(0, 0);
                }
                else
                {
                    parentTransform.SceneNode.RemoveChild(Light2D);
                }
                #endif
                UpdateEnabled();
            }

            public void ParentTo(ITransformComponent node)
            {
#if GODOT
                if (System != LightingSystem.Deferred)
                {
                    if (parentTransform != null)
                    {
                        DeParent();
                    }
                    ((IGodotTransformComponent)node).SceneNode.AddChild(Light2D);
                }
                parentTransform = (IGodotTransformComponent)node;
                UpdateEnabled();
#endif
            }

            public void Dispose()
            {
#if GODOT
// Already disposed.
                if (Light2D == null)
                {
                    return;
                }
                #endif

                Manager.RemoveLight(this);
                Manager = null;

#if GODOT
                Light2D.QueueFree();
                Light2D.Dispose();
                Light2D = null;
                #endif
            }

            private static ILightMode GetModeInstance(LightModeClass modeClass)
            {
                switch (modeClass)
                {
                    case LightModeClass.Constant:
                        return new LightModeConstant();

                    default:
                        throw new NotImplementedException("Light modes outside Constant are not implemented yet.");
                }
            }

            public void UpdateEnabled()
            {
#if GODOT
                Light2D.Visible = Enabled && Manager.Enabled && parentTransform != null;
                #endif
            }

            public void FrameProcess(FrameEventArgs args)
            {
#if GODOT
// TODO: Maybe use OnMove events to make this less expensive.
                if (System == LightingSystem.Deferred && parentTransform != null)
                {
                    var newpos = parentTransform.SceneNode.GlobalPosition;
                    if (CurrentPos != newpos)
                    {
                        Light2D.Position = newpos;
                    }
                }
                #endif
            }
        }
    }
}
