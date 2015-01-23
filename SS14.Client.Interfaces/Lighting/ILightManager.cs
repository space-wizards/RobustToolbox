using GorgonLibrary;
using SS14.Shared;
using System.Drawing;

namespace SS14.Client.Interfaces.Lighting
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
        ILight[] GetLights();
        void SetLightMode(LightModeClass? mode, ILight light);
    }
}