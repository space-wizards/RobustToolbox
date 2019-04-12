using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Shared.Enums;

namespace Robust.Client.Graphics.Lighting.LightMode
{
    class LightModeConstant : ILightMode
    {
        public LightModeClass ModeClass => LightModeClass.Constant;

        public void Shutdown()
        {
            // Nothing
        }

        public void Start(ILight owner)
        {
            // Nothing
        }

        public void Update(FrameEventArgs args)
        {
            // Nothing
        }
    }
}
