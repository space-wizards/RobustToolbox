using SS14.Shared;

namespace SS14.Client.Interfaces.Lighting
{
    public interface LightMode
    {
        LightModeClass LightModeClass { get; set; }
        void OnAdd(ILight owner);
        void OnRemove(ILight owner);
        void Update(ILight owner, float frametime);
    }
}