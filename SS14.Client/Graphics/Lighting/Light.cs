using System;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Utility;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Lighting
{
    class Light : ILight
    {
        public Vector2 Offset
        {
            get => Light2D.Position.Convert();
            set => Light2D.Position = value.Convert();
        }

        public Angle Rotation
        {
            get => new Angle(Light2D.GlobalRotation);
            set => Light2D.Rotation = (float)value;
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
                Light2D.Color = value.Convert();
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
                Light2D.TextureScale = value;
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
                Light2D.Energy = value;
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

        private TextureSource texture;
        public TextureSource Texture
        {
            get => texture;
            set
            {
                if (texture == value)
                {
                    return;
                }
                texture = value;
                Light2D.Texture = value;
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

        private Godot.Light2D Light2D;
        private ILightManager LightManager;

        public Light()
        {
            Light2D = new Godot.Light2D()
            {
                // TODO: Allow this to be modified.
                ShadowEnabled = true,
            };
            LightManager = IoCManager.Resolve<ILightManager>();
            Mode = new LightModeConstant();
            Mode.Start(this);
        }

        public void DeParent()
        {
            Light2D.GetParent().RemoveChild(Light2D);
        }

        public void ParentTo(Godot.Node node)
        {
            node.AddChild(Light2D);
        }

        public void Dispose()
        {
            // Already disposed.
            if (Light2D == null)
            {
                return;
            }

            Light2D.QueueFree();
            Light2D.Dispose();
            Light2D = null;
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
            Light2D.Visible = Enabled && LightManager.Enabled;
        }
    }
}
