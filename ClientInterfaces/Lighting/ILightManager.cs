using System.Collections.Generic;
using System.Drawing;
using ClientInterfaces.Map;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13_Shared;

namespace ClientInterfaces.Lighting
{
    public interface ILightManager
    {
        ILight CreateLight();
        void AddLight(ILight light);
        void RemoveLight(ILight light);
        ILight[] lightsInRadius(Vector2D point, float radius);
        void RecalculateLights();
        void RecalculateLightsInView(Vector2D point);
        void RecalculateLightsInView(RectangleF rect);
        ILight[] LightsIntersectingPoint(Vector2D point);
        ILight[] LightsIntersectingRect(RectangleF rect);
    }
}
