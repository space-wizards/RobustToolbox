using SS14.Shared;
using SS14.Shared.Enums;

namespace SS14.Client.Graphics.Lighting
{
    public interface LightMode
    {
        LightModeClass LightModeClass { get; }
        void OnAdd(ILight owner);
        void OnRemove(ILight owner);
        void Update(ILight owner, float deltaTime);
    }
}
