using SS14.Shared;
using System.Drawing;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Lighting
{
    public interface ILightManager
    {
        ILight CreateLight();
        void AddLight(ILight light);
        void RemoveLight(ILight light);
        ILight[] lightsInRadius(Vector2 point, float radius);
        void RecalculateLights();
        void RecalculateLightsInView(Vector2 point);
        void RecalculateLightsInView(RectangleF rect);
        ILight[] LightsIntersectingPoint(Vector2 point);
        ILight[] LightsIntersectingRect(RectangleF rect);
        ILight[] GetLights();
        void SetLightMode(LightModeClass? mode, ILight light);
    }
}