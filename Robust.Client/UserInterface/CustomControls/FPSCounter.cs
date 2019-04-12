using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;

namespace Robust.Client.UserInterface.CustomControls
{
    public class FPSCounter : Label
    {
        protected override void Initialize()
        {
            base.Initialize();

            FontColorShadowOverride = Color.Black;
            ShadowOffsetXOverride = 1;
            ShadowOffsetYOverride = 1;

            MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void Update(ProcessFrameEventArgs args)
        {
            if (!VisibleInTree)
            {
                return;
            }

            var fps = IoCManager.Resolve<IGameTiming>().FramesPerSecondAvg;
            Text = $"FPS: {fps:N1}";
        }
    }
}
