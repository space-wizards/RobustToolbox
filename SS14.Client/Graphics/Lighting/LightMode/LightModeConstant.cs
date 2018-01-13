using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Shared;

namespace SS14.Client.Graphics.Lighting
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
