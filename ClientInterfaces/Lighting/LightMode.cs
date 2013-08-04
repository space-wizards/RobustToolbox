using SS13_Shared;

namespace ClientInterfaces.Lighting
{
    public interface LightMode
    {
        LightModeClass LightModeClass { get; set; }
        void OnAdd(ILight owner);
        void OnRemove(ILight owner);
        void Update(ILight owner, float frametime);
    }
}