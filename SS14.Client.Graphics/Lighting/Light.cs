using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics.Textures;
using SS14.Shared;
using SS14.Shared.Map;
using SS14.Shared.Enums;
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
        private bool _regenLightMap;

        public Texture Mask
        {
            get;
            set;
        }

        public RenderImage RenderTarget { get; private set; }

        public Color Color { get; set; }

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
                Calculated = false;
            }
        }
        
        public LightMode LightMode { get; set; }

        public LightState LightState
        {
            get => _lightState;
            set
            {
                if (_lightState != value)
                {
                    _lightState = value;
                    Calculated = false;
                }
            }
        }

        public int Radius
        {
            get => _radius;
            set
            {
                if (_radius != value)
                {
                    _radius = value;
                    _regenLightMap = true;
                }
            }
        }

        /// <summary>
        ///     Have the shadows been calculated for this light.
        /// </summary>
        public bool Calculated { get; set; }

        public Vector2 LightMapSize { get; set; }

        /// <summary>
        ///     World position coordinates of the light's center
        /// </summary>
        public Vector2 LightPosition { get; set; }

        public Light(int radius = 128)
        {
            Radius = radius;
            Color = Color.White;
            _lightState = LightState.On;
        }

        public void Update(float frametime)
        {
            if (_regenLightMap)
            {
                GenLightMapRt();
                _regenLightMap = false;
            }

            LightMode?.Update(this, frametime);
        }

        public void BeginDrawingShadowCasters()
        {
            RenderTarget.BeginDrawing();

            RenderTarget.Clear(new Color(0, 0, 0, 0));
        }

        public void EndDrawingShadowCasters()
        {
            RenderTarget.EndDrawing();
        }

        public Vector2 ToRelativePosition(Vector2 worldPosition)
        {
            return worldPosition - (CluwneLib.WorldToScreen(LightPosition) - LightMapSize * 0.5f);
        }

        /// <summary>
        ///     Generates the Render Target of the light map.
        /// </summary>
        private void GenLightMapRt()
        {
            var shadowmapSize = RadiusToMapSize(_radius);
            var baseSize = 2 << (int) shadowmapSize;
            LightMapSize = new Vector2(baseSize, baseSize);

            RenderTarget?.Dispose();
            RenderTarget = new RenderImage("LightArea" + shadowmapSize, (uint) baseSize, (uint) baseSize);
        }

        private static ShadowmapSize RadiusToMapSize(int radius)
        {
            if (radius <= 128)
                return ShadowmapSize.Size128;

            if (radius <= 256)
                return ShadowmapSize.Size256;

            if (radius <= 512)
                return ShadowmapSize.Size512;

            return ShadowmapSize.Size1024;
        }
    }
}
