using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Client.Utility;
using Robust.Shared;
using Robust.Shared.Maths;
using System;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects.Components;

namespace Robust.Client.Graphics.Lighting
{
    partial class LightManager
    {
        sealed class Light : ILight
        {
            public Vector2 Offset
            {
                get => GameController.OnGodot ? Light2D.Offset.Convert() : default;
                set
                {
                    if (GameController.OnGodot)
                    {
                        Light2D.Offset = value.Convert();
                    }
                }
            }

            public Angle Rotation
            {
                get => GameController.OnGodot ? new Angle(Light2D.GlobalRotation) : default;
                set
                {
                    if (GameController.OnGodot)
                    {
                        Light2D.GlobalRotation = (float) value.Theta;
                    }
                }
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

                    if (GameController.OnGodot)
                    {
                        Light2D.Color = value.Convert();
                    }
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

                    if (GameController.OnGodot)
                    {
                        Light2D.TextureScale = value;
                    }
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
                    if (GameController.OnGodot)
                    {
                        Light2D.Energy = value;
                    }
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
                    if (GameController.OnGodot)
                    {
                        Light2D.Texture = value;
                    }
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

            public bool Disposed { get; private set; }

            private LightManager Manager;
            private LightingSystem System => Manager.System;
            private Godot.Light2D Light2D;
            private IGodotTransformComponent parentTransform;
            private Godot.Vector2 CurrentPos;

            public Light(LightManager manager)
            {
                Manager = manager;

                if (GameController.OnGodot)
                {
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
                }

                Mode = new LightModeConstant();
                Mode.Start(this);

                if (GameController.OnGodot && System == LightingSystem.Deferred)
                {
                    Manager.deferredViewport.AddChild(Light2D);
                }
            }

            public void DeParent()
            {
                if (GameController.OnGodot)
                {
                    if (System == LightingSystem.Deferred)
                    {
                        Light2D.Position = new Godot.Vector2(0, 0);
                    }
                    else
                    {
                        parentTransform.SceneNode.RemoveChild(Light2D);
                    }
                }

                UpdateEnabled();
            }

            public void ParentTo(ITransformComponent node)
            {
                if (!GameController.OnGodot)
                {
                    return;
                }

                if (System != LightingSystem.Deferred)
                {
                    if (parentTransform != null)
                    {
                        DeParent();
                    }

                    ((IGodotTransformComponent) node).SceneNode.AddChild(Light2D);
                }

                parentTransform = (IGodotTransformComponent) node;
                UpdateEnabled();
            }

            public void Dispose()
            {
                // Already disposed.
                if (Disposed)
                {
                    return;
                }


                Manager.RemoveLight(this);

                Disposed = true;

                if (!GameController.OnGodot)
                {
                    return;
                }

                Light2D.QueueFree();
                Light2D.Dispose();
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
                if (GameController.OnGodot)
                {
                    Light2D.Visible = Enabled && Manager.Enabled && parentTransform != null;
                }
            }

            public void FrameProcess(FrameEventArgs args)
            {
// TODO: Maybe use OnMove events to make this less expensive.
                if (!GameController.OnGodot || Manager.System != LightingSystem.Deferred ||
                    parentTransform == null)
                {
                    return;
                }

                var newpos = parentTransform.SceneNode.GlobalPosition;
                if (CurrentPos != newpos)
                {
                    Light2D.Position = newpos;
                }
            }
        }
    }
}
