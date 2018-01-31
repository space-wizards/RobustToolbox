using SS14.Shared.Map;
using SS14.Shared.Enums;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Lighting
{
    public interface ILightManager
    {
        ILight CreateLight();
        void AddLight(ILight light);
        void RemoveLight(ILight light);
        void RecalculateLights();
        void RecalculateLightsInView(MapId mapId, Box2 rect);
        ILight[] LightsIntersectingRect(MapId mapId, Box2 rect);
        ILight[] GetLights();
        void SetLightMode(LightModeClass? mode, ILight light);
        void Initialize();
    }
}
