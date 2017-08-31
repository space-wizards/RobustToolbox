using OpenTK;
using OpenTK.Graphics;
using SS14.Shared;
using SS14.Shared.IoC;

namespace SS14.Client.Graphics.Lighting
{
    public class Light : ILight
    {
        private LightState lightState = LightState.On;

        private Vector2 position;

        private int radius;

        public Light()
        {
            Radius = 256;
        }

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

        public int Radius
        {
            get => radius;
            set
            {
                if (radius != value)
                {
                    radius = value;
                    LightArea = new LightArea(RadiusToShadowMapSize(radius), IoCManager.Resolve<ILightManager>().LightMask);
                }
            }
        }

        public ILightArea LightArea { get; private set; }

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

        public void SetMask(SFML.Graphics.Sprite mask)
        {
            LightArea.SetMask(mask);
        }

        public void Update(float frametime)
        {
            LightMode?.Update(this);
        }

        public static ShadowmapSize RadiusToShadowMapSize(int Radius)
        {
            if (Radius <= 128)
                return ShadowmapSize.Size128;

            if (Radius <= 256)
                return ShadowmapSize.Size256;

            if (Radius <= 512)
                return ShadowmapSize.Size512;

            return ShadowmapSize.Size1024;
        }
    }
}
