using OpenTK;
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
        private LightState _lightState = LightState.On;

        private Vector2 _position;
        private MapId _mapID;
        private GridId _gridID;

        private int _radius;

        public Light()
        {
            Radius = 256;
        }

        public LocalCoordinates Coordinates
        {
            get => new LocalCoordinates(_position, _gridID, _mapID);
            set
            {
                if (_position == value.Position && _mapID == value.MapID && _gridID == value.GridID)
                    return;

                _position = value.Position;
                _mapID = value.MapID;
                _gridID = value.GridID;
                LightArea.Calculated = false;
            }
        }

        public Color Color { get; set; }
        public Vector4 ColorVec => new Vector4(Color.R, Color.G, Color.B, Color.A);

        public int Radius
        {
            get => _radius;
            set
            {
                if (_radius != value)
                {
                    _radius = value;
                    LightArea = new LightArea(RadiusToShadowMapSize(_radius), IoCManager.Resolve<ILightManager>().LightMask);
                }
            }
        }

        public ILightArea LightArea { get; private set; }

        public LightState LightState
        {
            get => _lightState;
            set
            {
                if (_lightState != value)
                {
                    _lightState = value;
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
