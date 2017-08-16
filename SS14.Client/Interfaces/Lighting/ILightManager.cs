using OpenTK;
using SFML.Graphics;
using SFML.System;
using SS14.Shared;
using SS14.Shared.IoC;

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
        void RecalculateLightsInView(Box2 rect);
        ILight[] LightsIntersectingPoint(Vector2 point);
        ILight[] LightsIntersectingRect(Box2 rect);
        ILight[] GetLights();
        void SetLightMode(LightModeClass? mode, ILight light);
    }
}
