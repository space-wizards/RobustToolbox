using OpenTK;
using OpenTK.Graphics;
using SS14.Shared;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Graphics.Lighting
{
    public interface ILight
    {
        int Radius { get; set; }
        Color4 Color { get; set; }
        Vector4 ColorVec { get; }
        LocalCoordinates Coordinates { get; set; }
        LightState LightState { get; set; }
        ILightArea LightArea { get; }
        LightMode LightMode { get; set; }
        void Update(float frametime);
        void SetMask(SFML.Graphics.Sprite _mask);
    }
}
