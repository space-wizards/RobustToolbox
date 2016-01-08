using SS14.Shared;
using System.Drawing;
using SS14.Shared.Maths;
using SFML.System;
using SFML.Graphics;

namespace SS14.Client.Interfaces.Lighting
{
    public interface ILightManager
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