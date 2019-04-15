using Robust.Shared;
using Robust.Shared.Enums;

namespace Robust.Client.Interfaces.Graphics.Lighting
{
    public interface ILightMode
    {
        void Start(ILight owner);
        void Shutdown();
        void Update(FrameEventArgs args);

        LightModeClass ModeClass { get; }
    }
}
