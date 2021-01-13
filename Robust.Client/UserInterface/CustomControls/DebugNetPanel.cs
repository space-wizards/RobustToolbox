using System;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    public class DebugNetPanel : PanelContainer
    {
        // Float so I don't have to cast it to prevent integer division down below.
        const float ONE_KIBIBYTE = 1024;

        private readonly IClientNetManager NetManager;
        private readonly IGameTiming GameTiming;

        private TimeSpan LastUpdate;
        private Label contents;

        // These are ints in the stats.
        // That's probably gonna get refactored at some point because > 2 GiB bandwidth usage isn't unreasonable, is it?
        private long LastSentBytes;
        private long LastReceivedBytes;

        private long LastSentPackets;
        private long LastReceivedPackets;

        public DebugNetPanel(IClientNetManager netMan, IGameTiming gameTiming)
        {
            NetManager = netMan;
            GameTiming = gameTiming;

            contents = new Label();

            SizeFlagsHorizontal = SizeFlags.None;

            contents = new Label
            {
                FontColorShadowOverride = Color.Black,
            };
            AddChild(contents);

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new Color(255, 105, 67, 138),
                ContentMarginLeftOverride = 5,
                ContentMarginTopOverride = 5
            };

            MouseFilter = contents.MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void Update(FrameEventArgs args)
        {
            base.Update(args);

            if ((GameTiming.RealTime - LastUpdate).Seconds < 1 || !VisibleInTree)
            {
                return;
            }

            if (!VisibleInTree)
            {
                return;
            }

            if (!NetManager.IsConnected)
            {
                contents.Text = "Not connected to server.";
                MinimumSizeChanged();
                return;
            }

            LastUpdate = GameTiming.RealTime;

            var stats = NetManager.Statistics;
            var sentBytes = stats.SentBytes - LastSentBytes;
            var receivedBytes = stats.ReceivedBytes - LastReceivedBytes;
            var sentPackets = stats.SentPackets - LastSentPackets;
            var receivedPackets = stats.ReceivedPackets - LastReceivedPackets;

            LastSentBytes = stats.SentBytes;
            LastReceivedBytes = stats.ReceivedBytes;
            LastSentPackets = stats.SentPackets;
            LastReceivedPackets = stats.ReceivedPackets;

            contents.Text = $@"UP: {sentBytes / ONE_KIBIBYTE:N} KiB/s, {sentPackets} pckt/s, {LastSentBytes / ONE_KIBIBYTE:N} KiB, {LastSentPackets} pckt
DOWN: {receivedBytes / ONE_KIBIBYTE:N} KiB/s, {receivedPackets} pckt/s, {LastReceivedBytes / ONE_KIBIBYTE:N} KiB, {LastReceivedPackets} pckt
PING: {NetManager.ServerChannel?.Ping ?? -1} ms";

            MinimumSizeChanged();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new(contents.CombinedMinimumSize.X + 10, contents.CombinedMinimumSize.Y + 10);
        }
    }
}
