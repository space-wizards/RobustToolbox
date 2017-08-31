using OpenTK;
using OpenTK.Graphics;
using SS14.Shared;

namespace SS14.Client.Graphics.Lighting
{
    public interface ILight
    {
        int Radius { get; set; }
        Color4 Color { get; set; }
        Vector4 ColorVec { get; }
        Vector2 Position { get; set; }
        LightState LightState { get; set; }
        ILightArea LightArea { get; }
        LightMode LightMode { get; set; }
        void Update(float frametime);
        void SetMask(SFML.Graphics.Sprite _mask);
    }
}
