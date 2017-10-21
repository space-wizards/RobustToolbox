using OpenTK;
using SS14.Shared;
using SS14.Client.Graphics.Sprites;

namespace SS14.Client.Graphics.Lighting
{
    public interface ILightManager
    {
        Sprite LightMask { get; set; }
        ILight CreateLight();
        void AddLight(ILight light);
        void RemoveLight(ILight light);
        void RecalculateLights();
        void RecalculateLightsInView(Box2 rect);
        ILight[] LightsIntersectingRect(Box2 rect);
        ILight[] GetLights();
        void SetLightMode(LightModeClass? mode, ILight light);
        void Initialize();
    }
}
