using System;
using System.Linq;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    public class DebugNetBandwidthPanel : PanelContainer
    {
        private const int OneKibibyte = 1024;

        private readonly IClientNetManager _netManager;
        private readonly IGameTiming _gameTiming;

        private TimeSpan _lastUpdate;
        private readonly Label _contents;

        public DebugNetBandwidthPanel(IClientNetManager netMan, IGameTiming gameTiming)
        {
            _netManager = netMan;
            _gameTiming = gameTiming;

            _contents = new Label();

            SizeFlagsHorizontal = SizeFlags.None;

            _contents = new Label
            {
                FontColorShadowOverride = Color.Black,
            };
            AddChild(_contents);

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new Color(255, 105, 67, 138),
                ContentMarginLeftOverride = 5,
                ContentMarginTopOverride = 5
            };

            MouseFilter = _contents.MouseFilter = MouseFilterMode.Ignore;
            Visible = false;
        }

        protected override void Update(FrameEventArgs args)
        {
            base.Update(args);

            if ((_gameTiming.RealTime - _lastUpdate).Seconds < 1 || !VisibleInTree)
            {
                return;
            }

            _lastUpdate = _gameTiming.RealTime;

            var bandwidth = _netManager.MessageBandwidthUsage
                .OrderByDescending(p => p.Value)
                .Select(p => $"{TypeAbbreviation.Abbreviate(p.Key)}: {p.Value / OneKibibyte} KiB");

            _contents.Text = string.Join('\n', bandwidth);

            MinimumSizeChanged();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new(_contents.CombinedMinimumSize.X + 10, _contents.CombinedMinimumSize.Y + 10);
        }
    }
}
