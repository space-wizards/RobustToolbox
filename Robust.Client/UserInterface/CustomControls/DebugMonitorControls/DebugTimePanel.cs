using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Timing;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls.DebugMonitorControls
{
    internal sealed class DebugTimePanel : PanelContainer
    {
        private readonly IClientGameTiming _gameTiming;
        private readonly IClientGameStateManager _gameState;

        private readonly char[] _textBuffer = new char[256];
        private readonly Label _contents;

        public DebugTimePanel(IClientGameTiming gameTiming, IClientGameStateManager gameState)
        {
            _gameTiming = gameTiming;
            _gameState = gameState;

            _contents = new Label
            {
                FontColorShadowOverride = Color.Black,
            };
            AddChild(_contents);

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new Color(35, 134, 37, 138),
                ContentMarginLeftOverride = 5,
                ContentMarginTopOverride = 5
            };

            MouseFilter = _contents.MouseFilter = MouseFilterMode.Ignore;

            HorizontalAlignment = HAlignment.Left;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!VisibleInTree)
            {
                return;
            }

            // NOTE: CurTick gets incremented by the main loop AFTER Tick runs, not before.
            // This means that CurTick reports the NEXT tick to be ran, NOT the last tick that was ran.
            // This is why there's a -1 on Pred:.

            _contents.TextMemory = FormatHelpers.FormatIntoMem(_textBuffer,
                $@"Paused: {_gameTiming.Paused}, CurTick: {_gameTiming.CurTick}, LastProcessed: {_gameTiming.LastProcessedTick}, LastRealTick: {_gameTiming.LastRealTick}, Pred: {_gameTiming.CurTick.Value - _gameTiming.LastRealTick.Value - 1}
CurTime: {_gameTiming.CurTime:d\:hh\:mm\:ss\.ff}, RealTime: {_gameTiming.RealTime:d\:hh\:mm\:ss\.ff}, CurFrame: {_gameTiming.CurFrame}
ServerTime: {_gameTiming.ServerTime:d\:hh\:mm\:ss\.ff}, TickTimingAdjustment: {_gameTiming.TickTimingAdjustment}, TickRate: {_gameTiming.TickRate}");
        }
    }
}
