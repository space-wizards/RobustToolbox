using SS14.Client.Graphics.Drawing;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface.CustomControls
{
    public class DebugTimePanel : Panel
    {
        [Dependency]
        private readonly IResourceCache _resourceCache;

        [Dependency]
        private readonly IGameTiming _gameTiming;

        private Label _contents;

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);

            _contents = new Label
            {
                FontOverride = _resourceCache.GetResource<FontResource>(new ResourcePath("/Fonts/CALIBRI.TTF"))
                    .MakeDefault(),
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

        protected override void Update(ProcessFrameEventArgs args)
        {
            base.Update(args);

            if (!VisibleInTree)
            {
                return;
            }

            _contents.Text = $@"Paused: {_gameTiming.Paused}, CurTick: {_gameTiming.CurTick},
CurTime: {_gameTiming.CurTime}, RealTime: {_gameTiming.RealTime}";

            MinimumSizeChanged();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(_contents.CombinedMinimumSize.X + 10, _contents.CombinedMinimumSize.Y + 10);
        }
    }
}
