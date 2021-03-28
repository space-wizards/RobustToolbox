using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    internal sealed class FpsCounter : Label
    {
        private readonly IGameTiming _gameTiming;

        public FpsCounter(IGameTiming gameTiming)
        {
            _gameTiming = gameTiming;

            FontColorShadowOverride = Color.Black;
            ShadowOffsetXOverride = 1;
            ShadowOffsetYOverride = 1;
        }

        protected override void FrameUpdate(FrameEventArgs args)
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
