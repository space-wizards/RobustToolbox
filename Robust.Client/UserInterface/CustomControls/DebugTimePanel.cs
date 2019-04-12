using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Client.UserInterface.Controls;
using Robust.Client.ResourceManagement;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
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
CurTime: {_gameTiming.CurTime}, RealTime: {_gameTiming.RealTime}, CurFrame: {_gameTiming.CurFrame}";

            MinimumSizeChanged();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(_contents.CombinedMinimumSize.X + 10, _contents.CombinedMinimumSize.Y + 10);
        }
    }
}
