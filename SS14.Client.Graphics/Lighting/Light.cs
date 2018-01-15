using OpenTK;
using OpenTK.Graphics;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;
using SS14.Client.Graphics.Sprites;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Lighting
{
    public class Light : ILight
    {
        private LightState lightState = LightState.On;

        private Vector2 position;
        private MapId MapID;
        private GridId GridID;

        private int radius;

        public Light()
        {
            Radius = 256;
        }

        public LocalCoordinates Coordinates
        {
            get => new LocalCoordinates(position, GridID, MapID);
            set
            {
                if (position != value.Position || MapID != value.MapID || GridID != value.GridID)
                {
                    position = value.Position;
                    MapID = value.MapID;
                    GridID = value.GridID;
                    LightArea.Calculated = false;
                }
            }
        }

        public Color Color { get; set; }
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

        public void SetMask(Sprite mask)
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
