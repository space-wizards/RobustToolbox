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
                get => default;
                set
                {
                }
            }

            public Angle Rotation
            {
                get => default;
                set
                {
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

            public Light(LightManager manager)
            {
                Manager = manager;

                Mode = new LightModeConstant();
                Mode.Start(this);
            }

            public void DeParent()
            {
                UpdateEnabled();
            }

            public void ParentTo(ITransformComponent node)
            {
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
            }

            public void FrameProcess(FrameEventArgs args)
            {
            }
        }
    }
}
