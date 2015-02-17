using SS14.Client.Graphics.CluwneLib;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.Lighting;
using SS14.Shared;
using System.Drawing;

namespace SS14.Client.Services.Lighting
{
    public class Light : ILight
    {
        public Light()
        {
            Radius = 256;
            LightState = LightState.On;
        }

        #region ILight Members

        public Vector2 Position { get; private set; }
        public Color Color { get; private set; }
        public int Radius { get; private set; }
        public ILightArea LightArea { get; private set; }
        public LightState LightState { get; private set; }
        public LightMode LightMode { get; set; }

        public void Move(Vector2 toPosition)
        {
            Position = toPosition;
            LightArea.Calculated = false;
        }

        public void SetRadius(int radius)
        {
            if (Radius != radius)
            {
                Radius = radius;
                LightArea = new LightArea(RadiusToShadowMapSize(Radius));
            }
        }

        public void SetColor(int a, int r, int g, int b)
        {
            Color = Color.FromArgb(a, r, g, b);
        }

        public void SetColor(Color color)
        {
            Color = color;
        }

        public Vector4 GetColorVec()
        {
            return new Vector4((float) Color.R/255, (float) Color.G/255, (float) Color.B/255, (float) Color.A/255);
        }

        public void SetMask(string mask)
        {
            LightArea.SetMask(mask);
        }

        public void Update(float frametime)
        {
            if (LightMode != null) LightMode.Update(this, frametime);
        }

        public void SetState(LightState state)
        {
            LightState = state;
            LightArea.Calculated = false;
        }

        #endregion

        public static ShadowmapSize RadiusToShadowMapSize(int Radius)
        {
            switch (Radius)
            {
                case 128:
                    return ShadowmapSize.Size128;
                case 256:
                    return ShadowmapSize.Size256;
                case 512:
                    return ShadowmapSize.Size512;
                case 1024:
                    return ShadowmapSize.Size1024;
                default:
                    return ShadowmapSize.Size1024;
            }
        }
    }
}