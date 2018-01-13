using OpenTK;
using SS14.Shared;
using SS14.Shared.Map;
using SS14.Client.Graphics.Sprites;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Lighting
{
    public interface ILight
    {
        int Radius { get; set; }
        Color Color { get; set; }
        Vector4 ColorVec { get; }
        LocalCoordinates Coordinates { get; set; }
        LightState LightState { get; set; }
        ILightArea LightArea { get; }
        LightMode LightMode { get; set; }
        void Update(float frametime);
        void SetMask(Sprite _mask);
    }
}
