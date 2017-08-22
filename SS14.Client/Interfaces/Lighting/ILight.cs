using OpenTK;
using OpenTK.Graphics;
using SS14.Shared;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Lighting
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
        void SetMask(string _mask);
    }
}
