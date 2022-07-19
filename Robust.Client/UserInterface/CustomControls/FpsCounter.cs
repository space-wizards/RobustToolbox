using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class FpsCounter : Label
    {
        private readonly IGameTiming _gameTiming;

        private readonly char[] _textBuffer = new char[16];

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
            TextMemory = FormatHelpers.FormatIntoMem(_textBuffer, $"FPS: {fps:N0}");
        }
    }
}
