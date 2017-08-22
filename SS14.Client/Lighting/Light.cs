using OpenTK;
using OpenTK.Graphics;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Interfaces.Lighting;
using SS14.Shared;
using SS14.Shared.Maths;

namespace SS14.Client.Lighting
{
    public class Light : ILight
    {
        public Light()
        {
            Radius = 256;
        }

        #region ILight Members

        private Vector2 position;
        public Vector2 Position
        {
            get => position;
            set
            {
                if (position != value)
                {
                    position = value;
                    LightArea.Calculated = false;
                }
            }
        }

        public Color4 Color { get; set; }
        public Vector4 ColorVec => new Vector4(Color.R, Color.G, Color.B, Color.A);

        private int radius;
        public int Radius
        {
            get => radius;
            set
            {
                if (radius != value)
                {
                    radius = value;
                    LightArea = new LightArea(RadiusToShadowMapSize(radius));
                }
            }
        }

        public ILightArea LightArea { get; private set; }

        private LightState lightState = LightState.On;
        public LightState LightState
        {
            get => lightState;
            set
            {
                if (lightState != value)
                {
                    lightState = value;
                    LightArea.Calculated = false;
                }
            }
        }

        public LightMode LightMode { get; set; }

        public void SetMask(string mask)
        {
            LightArea.SetMask(mask);
        }

        public void Update(float frametime)
        {
            LightMode?.Update(this, frametime);
        }

        #endregion

        public static ShadowmapSize RadiusToShadowMapSize(int Radius)
        {
            if (Radius <= 128)
            {
                return ShadowmapSize.Size128;
            }

            if (Radius <= 256)
            {
                return ShadowmapSize.Size256;
            }

            if (Radius <= 512)
            {
                return ShadowmapSize.Size512;
            }

            return ShadowmapSize.Size1024;
        }
    }
}
