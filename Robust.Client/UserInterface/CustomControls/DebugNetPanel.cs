using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;

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

            HorizontalAlignment = HAlignment.Left;

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

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

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

            // MinimumSizeChanged();
        }
    }
}
