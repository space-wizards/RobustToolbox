using System.Collections.Generic;
using System.Drawing;
using GorgonLibrary;

namespace ClientInterfaces.Lighting
{
    public interface ILight
    {
        int Radius {get;}
        Color Color { get; }
        Vector2D Position { get; }
        void Move(Vector2D toPosition);
        void SetRadius(int Radius);
        void SetColor(int a, int r, int g, int b);
        void SetColor(Color color);
        ILightArea LightArea { get; }
    }
}
