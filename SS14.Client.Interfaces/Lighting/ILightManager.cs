using SFML.Graphics;
using SFML.System;
using SS14.Shared;
using SS14.Shared.IoC;

namespace SS14.Client.Interfaces.Lighting
{
    public interface ILightManager : IIoCInterface
    {
        ILight CreateLight();
        void AddLight(ILight light);
        void RemoveLight(ILight light);
        ILight[] lightsInRadius(Vector2f point, float radius);
        void RecalculateLights();
        void RecalculateLightsInView(Vector2f point);
        void RecalculateLightsInView(FloatRect rect);
        ILight[] LightsIntersectingPoint(Vector2f point);
        ILight[] LightsIntersectingRect(FloatRect rect);
        ILight[] GetLights();
        void SetLightMode(LightModeClass? mode, ILight light);
    }
}
