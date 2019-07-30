using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.CustomControls
{
    internal sealed class FpsCounter : Label
    {
        private readonly IGameTiming _gameTiming;

        public FpsCounter(IGameTiming gameTiming)
        {
            _gameTiming = gameTiming;
        }

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

            var fps = _gameTiming.FramesPerSecondAvg;
            Text = $"FPS: {fps:N1}";
        }
    }
}
