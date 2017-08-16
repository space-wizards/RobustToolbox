using OpenTK;
using SFML.Graphics;
using SFML.System;
using SS14.Shared;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Lighting
{
    public interface ILight
    {
        int Radius { get; }
        Color Color { get; }
        Vector2 Position { get; }
        LightState LightState { get; }
        ILightArea LightArea { get; }
        LightMode LightMode { get; set; }
        void Move(Vector2 toPosition);
        void SetRadius(int Radius);
        void SetColor(int a, int r, int g, int b);
        void SetColor(Color color);

        void Update(float frametime);

        void SetMask(string _mask);
        Vector4 GetColorVec();
        void SetState(LightState state);
    }
}
