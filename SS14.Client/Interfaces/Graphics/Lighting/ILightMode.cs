using SS14.Shared;
using SS14.Shared.Enums;

namespace SS14.Client.Interfaces.Graphics.Lighting
{
    public interface ILightMode
    {
        void Start(ILight owner);
        void Shutdown();
        void Update(FrameEventArgs args);

        LightModeClass ModeClass { get; }
    }
}
