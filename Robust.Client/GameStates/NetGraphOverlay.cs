using System;
using System.Collections.Generic;
using System.Text;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Robust.Client.GameStates
{
    /// <summary>
    ///     Visual debug overlay for the network diagnostic graph.
    /// </summary>
    internal class NetGraphOverlay : Overlay
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IClientGameStateManager _gameStateManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;

        private const int HistorySize = 60 * 3; // number of ticks to keep in history.
        private const int TargetPayloadBps = 56000 / 8; // Target Payload size in Bytes per second. A mind-numbing fifty-six thousand bits per second, who would ever need more?
        private const int MidrangePayloadBps = 33600 / 8; // mid-range line
        private const int BytesPerPixel = 2; // If you are running the game on a DSL connection, you can scale the graph to fit your absurd bandwidth.
        private const int LowerGraphOffset = 100; // Offset on the Y axis in pixels of the lower lag/interp graph.
        private const int MsPerPixel = 4; // Latency Milliseconds per pixel, for scaling the graph.

        /// <inheritdoc />
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        private readonly Font _font;
        private int _warningPayloadSize;
        private int _midrangePayloadSize;

        private readonly List<(GameTick Tick, int Payload, int lag, int interp)> _history = new(HistorySize+10);

        private int _totalHistoryPayload; // sum of all data point sizes in bytes

        public EntityUid WatchEntId { get; set; }

        public NetGraphOverlay()
        {
            IoCManager.InjectDependencies(this);
            var cache = IoCManager.Resolve<IResourceCache>();
            _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);

            _gameStateManager.GameStateApplied += HandleGameStateApplied;
        }

        private void HandleGameStateApplied(GameStateAppliedArgs args)
        {
            var toSeq = args.AppliedState.ToSequence;
            var sz = args.AppliedState.PayloadSize;

            // calc payload size
            _warningPayloadSize = TargetPayloadBps / _gameTiming.TickRate;
            _midrangePayloadSize = MidrangePayloadBps / _gameTiming.TickRate;

            // calc lag
            var lag = _netManager.ServerChannel!.Ping;

            // calc interp info
            var interpBuff = _gameStateManager.CurrentBufferSize - _gameStateManager.MinBufferSize;

            _totalHistoryPayload += sz;
            _history.Add((toSeq, sz, lag, interpBuff));

            // not watching an ent
            if(!WatchEntId.IsValid() || WatchEntId.IsClientSide())
                return;

            string? entStateString = null;
            string? entDelString = null;
            var conShell = IoCManager.Resolve<IConsoleHost>().LocalShell;

            var entStates = args.AppliedState.EntityStates;
            if (entStates is not null)
            {
                var sb = new StringBuilder();
                foreach (var entState in entStates)
                {
                    if (entState.Uid == WatchEntId)
                    {
                        if(entState.ComponentChanges is not null)
                        {
                            sb.Append($"\n  Changes:");
                            foreach (var compChange in entState.ComponentChanges)
                            {
                                var registration = _componentFactory.GetRegistration(compChange.NetID);
                                var del = compChange.Deleted ? 'D' : 'C';
                                sb.Append($"\n    [{del}]{compChange.NetID}:{registration.Name}");
                            }
                        }

                        if (entState.ComponentStates is not null)
                        {
                            sb.Append($"\n  States:");
                            foreach (var compState in entState.ComponentStates)
                            {
                                sb.Append($"\n    {compState.NetID}:{compState.GetType().Name}");
                            }
                        }
                    }
                }
                entStateString = sb.ToString();
            }

            var entDeletes = args.AppliedState.EntityDeletions;
            if (entDeletes is not null)
            {
                var sb = new StringBuilder();
                foreach (var entDelete in entDeletes)
                {
                    if (entDelete == WatchEntId)
                    {
                        entDelString = "\n  Deleted";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(entStateString) || !string.IsNullOrWhiteSpace(entDelString))
            {
                var fullString = $"watchEnt: from={args.AppliedState.FromSequence}, to={args.AppliedState.ToSequence}, eid={WatchEntId}";
                if (!string.IsNullOrWhiteSpace(entStateString))
                    fullString += entStateString;

                if (!string.IsNullOrWhiteSpace(entDelString))
                    fullString += entDelString;

                conShell.WriteLine(fullString + "\n");
            }

        }

        /// <inheritdoc />
        protected internal override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            var over = _history.Count - HistorySize;
            if (over <= 0)
                return;

            for (int i = 0; i < over; i++)
            {
                var point = _history[i];
                _totalHistoryPayload -= point.Payload;
            }

            _history.RemoveRange(0, over);
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            // remember, 0,0 is top left of ui with +X right and +Y down

            var leftMargin = 300;
            var width = HistorySize;
            var height = 500;
            var drawSizeThreshold = Math.Min(_totalHistoryPayload / HistorySize, 300);
            var handle = args.ScreenHandle;

            // bottom payload line
            handle.DrawLine(new Vector2(leftMargin, height), new Vector2(leftMargin + width, height), Color.DarkGray.WithAlpha(0.8f));

            // bottom lag line
            handle.DrawLine(new Vector2(leftMargin, height + LowerGraphOffset), new Vector2(leftMargin + width, height + LowerGraphOffset), Color.DarkGray.WithAlpha(0.8f));

            int lastLagY = -1;
            int lastLagMs = -1;
            // data points
            for (var i = 0; i < _history.Count; i++)
            {
                var state = _history[i];

                // draw the payload size
                var xOff = leftMargin + i;
                var yoff = height - state.Payload / BytesPerPixel;
                handle.DrawLine(new Vector2(xOff, height), new Vector2(xOff, yoff), Color.LightGreen.WithAlpha(0.8f));

                // Draw size if above average
                if (drawSizeThreshold * 1.5 < state.Payload)
                {
                    handle.DrawString(_font, new Vector2(xOff, yoff - _font.GetLineHeight(1)), state.Payload.ToString());
                }

                // second tick marks
                if (state.Tick.Value % _gameTiming.TickRate == 0)
                {
                    handle.DrawLine(new Vector2(xOff, height), new Vector2(xOff, height+2), Color.LightGray);
                }

                // lag data
                var lagYoff = height + LowerGraphOffset - state.lag / MsPerPixel;
                lastLagY = lagYoff - 1;
                lastLagMs = state.lag;
                handle.DrawLine(new Vector2(xOff, lagYoff - 2), new Vector2(xOff, lagYoff - 1), Color.Blue.WithAlpha(0.8f));

                // interp data
                Color interpColor;
                if(state.interp < 0)
                    interpColor = Color.Red;
                else if(state.interp < _gameStateManager.TargetBufferSize - _gameStateManager.MinBufferSize)
                    interpColor = Color.Yellow;
                else
                    interpColor = Color.Green;

                handle.DrawLine(new Vector2(xOff, height + LowerGraphOffset), new Vector2(xOff, height + LowerGraphOffset + state.interp * 6), interpColor.WithAlpha(0.8f));
            }

            // average payload line
            var avgyoff = height - drawSizeThreshold / BytesPerPixel;
            handle.DrawLine(new Vector2(leftMargin, avgyoff), new Vector2(leftMargin + width, avgyoff), Color.DarkGray.WithAlpha(0.8f));

            // top payload warning line
            var warnYoff = height - _warningPayloadSize / BytesPerPixel;
            handle.DrawLine(new Vector2(leftMargin, warnYoff), new Vector2(leftMargin + width, warnYoff), Color.DarkGray.WithAlpha(0.8f));

            // mid payload line
            var midYoff = height - _midrangePayloadSize / BytesPerPixel;
            handle.DrawLine(new Vector2(leftMargin, midYoff), new Vector2(leftMargin + width, midYoff), Color.DarkGray.WithAlpha(0.8f));

            // payload text
            handle.DrawString(_font, new Vector2(leftMargin + width, warnYoff), "56K");
            handle.DrawString(_font, new Vector2(leftMargin + width, midYoff), "33.6K");

            // interp text info
            if(lastLagY != -1)
                handle.DrawString(_font, new Vector2(leftMargin + width, lastLagY), $"{lastLagMs.ToString()}ms");

            handle.DrawString(_font, new Vector2(leftMargin, height + LowerGraphOffset), $"{_gameStateManager.CurrentBufferSize.ToString()} states");
        }

        protected override void DisposeBehavior()
        {
            _gameStateManager.GameStateApplied -= HandleGameStateApplied;

            base.DisposeBehavior();
        }

        private class NetShowGraphCommand : IConsoleCommand
        {
            public string Command => "net_graph";
            public string Help => "net_graph <0|1>";
            public string Description => "Toggles the net statistics pannel.";

            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                if (args.Length != 1)
                {
                    shell.WriteError("Invalid argument amount. Expected 2 arguments.");
                    return;
                }

                if (!byte.TryParse(args[0], out var iValue))
                {
                    shell.WriteLine("Invalid argument: Needs to be 0 or 1.");
                    return;
                }

                var bValue = iValue > 0;
                var overlayMan = IoCManager.Resolve<IOverlayManager>();

                if(bValue && !overlayMan.HasOverlay(typeof(NetGraphOverlay)))
                {
                    overlayMan.AddOverlay(new NetGraphOverlay());
                    shell.WriteLine("Enabled network overlay.");
                }
                else if(overlayMan.HasOverlay(typeof(NetGraphOverlay)))
                {
                    overlayMan.RemoveOverlay(typeof(NetGraphOverlay));
                    shell.WriteLine("Disabled network overlay.");
                }
            }
        }

        private class NetWatchEntCommand : IConsoleCommand
        {
            public string Command => "net_watchent";
            public string Help => "net_watchent <0|EntityUid>";
            public string Description => "Dumps all network updates for an EntityId to the console.";

            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                if (args.Length != 1)
                {
                    shell.WriteError("Invalid argument amount. Expected 1 argument.");
                    return;
                }

                if (!EntityUid.TryParse(args[0], out var eValue))
                {
                    shell.WriteError("Invalid argument: Needs to be 0 or an entityId.");
                    return;
                }

                var overlayMan = IoCManager.Resolve<IOverlayManager>();

                if (overlayMan.HasOverlay(typeof(NetGraphOverlay)))
                {
                    var netOverlay = overlayMan.GetOverlay<NetGraphOverlay>();

                    netOverlay.WatchEntId = eValue;
                }
            }
        }
    }
}
