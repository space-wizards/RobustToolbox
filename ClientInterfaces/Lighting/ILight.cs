using System.Drawing;
using GorgonLibrary;
using SS13_Shared;

namespace ClientInterfaces.Lighting
{
    public interface ILight
    {
        int Radius { get; }
        Color Color { get; }
        Vector2D Position { get; }
        LightState LightState { get; }
        ILightArea LightArea { get; }
        LightMode LightMode { get; set; }
        void Move(Vector2D toPosition);
        void SetRadius(int Radius);
        void SetColor(int a, int r, int g, int b);
        void SetColor(Color color);

        void Update(float frametime);

        void SetMask(string _mask);
        Vector4D GetColorVec();
        void SetState(LightState state);
    }
}