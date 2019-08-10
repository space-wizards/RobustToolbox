using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    public class DebugTimePanel : Panel
    {
        private readonly IGameTiming _gameTiming;

        private Label _contents;

        public DebugTimePanel(IGameTiming gameTiming)
        {
            _gameTiming = gameTiming;

            PerformLayout();
        }

        protected override void Initialize()
        {
            base.Initialize();

            _contents = new Label();
        }

        private void PerformLayout()
        {
            _contents = new Label
            {
                FontColorShadowOverride = Color.Black,
                MarginTop = 5,
                MarginLeft = 5
            };
            AddChild(_contents);

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new Color(67, 105, 255, 138),
            };

            MouseFilter = _contents.MouseFilter = MouseFilterMode.Ignore;

            SizeFlagsHorizontal = SizeFlags.None;
        }

        protected override void Update(FrameEventArgs args)
        {
            base.Update(args);

            if (!VisibleInTree)
            {
                return;
            }

            _contents.Text = $@"Paused: {_gameTiming.Paused}, CurTick: {_gameTiming.CurTick},
CurTime: {_gameTiming.CurTime}, RealTime: {_gameTiming.RealTime}, CurFrame: {_gameTiming.CurFrame}";

            MinimumSizeChanged();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(_contents.CombinedMinimumSize.X + 10, _contents.CombinedMinimumSize.Y + 10);
        }
    }
}
