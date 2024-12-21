using System;
using System.Collections.Generic;
using System.Numerics;
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
using Robust.Client.Player;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates
{
    /// <summary>
    ///     Visual debug overlay for the network diagnostic graph.
    /// </summary>
    internal sealed class NetGraphOverlay : Overlay
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IClientGameStateManager _gameStateManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IConsoleHost _host = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;

        private const int HistorySize = 60 * 5; // number of ticks to keep in history.
        private const int TargetPayloadBps = 56000 / 8; // Target Payload size in Bytes per second. A mind-numbing fifty-six thousand bits per second, who would ever need more?
        private const int ExtremePayloadBps = 256000 / 8; // I know this is somewhat extreme, but I'm gonna make the graph go up to 256kbs. That's a whopping 32 KILO bytes per second.
        private const int MidrangePayloadBps = 19200 / 8; // mid-range line
        private const int BaselineBps = 8192 / 8; // mid-range line
        private const int LowerGraphOffset = 100; // Offset on the Y axis in pixels of the lower lag/interp graph.
        private const int LeftMargin = 500; // X offset, to avoid interfering with the f3 menu.
        private const int MsPerPixel = 4; // Latency Milliseconds per pixel, for scaling the graph.

        /// <inheritdoc />
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        private readonly Font _font;

        private readonly List<(GameTick Tick, int Payload, int lag, int Buffer)> _history = new(HistorySize+10);

        // sum of all data point sizes in bytes
        private int _totalHistoryPayload;

        public EntityUid WatchEntId { get; set; }

        public NetGraphOverlay()
        {
            IoCManager.InjectDependencies(this);
            var cache = IoCManager.Resolve<IResourceCache>();
            _font = new VectorFont(cache.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"), 10);

            _gameStateManager.GameStateApplied += HandleGameStateApplied;
        }

        private void HandleGameStateApplied(GameStateAppliedArgs args)
        {
            var toSeq = args.AppliedState.ToSequence;
            var sz = args.AppliedState.PayloadSize;

            // calc lag
            var lag = _netManager.ServerChannel!.Ping;

            // calc interp info
            var buffer = _gameStateManager.GetApplicableStateCount();

            _totalHistoryPayload += sz;
            _history.Add((toSeq, sz, lag, buffer));

            // not watching an ent
            if(!WatchEntId.IsValid() || _entManager.IsClientSide(WatchEntId))
                return;

            string? entStateString = null;
            string? entDelString = null;
            var conShell = _host.LocalShell;

            var entStates = args.AppliedState.EntityStates;
            if (entStates.HasContents)
            {
                var sb = new StringBuilder();
                foreach (var entState in entStates.Span)
                {
                    var uid = _entManager.GetEntity(entState.NetEntity);

                    if (uid != WatchEntId)
                        continue;

                    if (!entState.ComponentChanges.HasContents)
                    {
                        sb.Append("\n Entered PVS");
                        break;
                    }

                    sb.Append($"\n  Changes:");
                    foreach (var compChange in entState.ComponentChanges.Span)
                    {
                        var registration = _componentFactory.GetRegistration(compChange.NetID);
                        sb.Append($"\n    [{compChange.NetID}:{registration.Name}");

                        if (compChange.State is not null)
                            sb.Append($"\n      STATE:{compChange.State.GetType().Name}");
                    }

                    // Note that component deletion is now implicit via the list of network comp ids. So it currently
                    // doesn't get logged here.

                    break;
                }
                entStateString = sb.ToString();
            }

            foreach (var ent in args.Detached)
            {
                var uid = _entManager.GetEntity(ent);

                if (uid != WatchEntId)
                    continue;

                conShell.WriteLine($"watchEnt: Left PVS at tick {args.AppliedState.ToSequence}, eid={WatchEntId}" + "\n");
            }

            var entDeletes = args.AppliedState.EntityDeletions;
            if (entDeletes.HasContents)
            {
                foreach (var entDelete in entDeletes.Span)
                {
                    var uid = _entManager.GetEntity(entDelete);

                    if (uid == WatchEntId)
                        entDelString = "\n  Deleted";
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

        private static int BytesToPixels(int bytes)
        {
            // the minimum size seems to be about 10 bytes.
            // 10 bytes = 15 pixel, then doubling every 15 more pixels.
            bytes = Math.Max(bytes - 10, 1);
            return (int) Math.Round((1 + Math.Log2(bytes)) * 15);
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            // remember, 0,0 is top left of ui with +X right and +Y down

            var highPayload = BytesToPixels(ExtremePayloadBps / _gameTiming.TickRate);
            var targetPayload = BytesToPixels(TargetPayloadBps / _gameTiming.TickRate);
            var midPayload = BytesToPixels(MidrangePayloadBps / _gameTiming.TickRate);
            var minPayload = BytesToPixels(BaselineBps / _gameTiming.TickRate);
            var width = HistorySize;
            var height = 500;
            var handle = args.ScreenHandle;

            // bottom payload line
            handle.DrawLine(new Vector2(LeftMargin, height), new Vector2(LeftMargin + width, height), Color.DarkGray.WithAlpha(0.8f));

            // bottom lag line
            handle.DrawLine(new Vector2(LeftMargin, height + LowerGraphOffset), new Vector2(LeftMargin + width, height + LowerGraphOffset), Color.DarkGray.WithAlpha(0.8f));

            int lastLagY = -1;
            int lastLagMs = -1;
            // data points
            for (var i = 0; i < _history.Count; i++)
            {
                var state = _history[i];

                // draw the uncompressed size
                var xOff = LeftMargin + i;
                var yoff = height - BytesToPixels(state.Payload);
                handle.DrawLine(new Vector2(xOff, height), new Vector2(xOff, yoff), Color.LimeGreen.WithAlpha(0.8f));

                // second tick marks
                if (state.Tick.Value % (_gameTiming.TickRate/5) == 0)
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
                if(state.Buffer < _gameStateManager.MinBufferSize)
                    interpColor = Color.Red;
                else if(state.Buffer < _gameStateManager.TargetBufferSize)
                    interpColor = Color.Yellow;
                else
                    interpColor = Color.Green;

                var delta = (state.Buffer - _gameStateManager.MinBufferSize);
                handle.DrawLine(new Vector2(xOff, height + LowerGraphOffset), new Vector2(xOff, height + LowerGraphOffset +  delta * 6), interpColor.WithAlpha(0.8f));
            }

            // average payload line
            var avg = height - BytesToPixels(_totalHistoryPayload/HistorySize);
            var avgEnd = new Vector2(LeftMargin + width, avg) + new Vector2(70, 0);
            handle.DrawLine(new Vector2(LeftMargin, avg), avgEnd, Color.DarkGray.WithAlpha(0.8f));

            // top payload warning line
            var warnYoff = height - targetPayload;
            handle.DrawLine(new Vector2(LeftMargin, warnYoff), new Vector2(LeftMargin + width, warnYoff), Color.DarkGray.WithAlpha(0.8f));

            // Extreme payload hazard line -- their modem will probably explode at this point.
            var extremeYoff = height - highPayload;
            handle.DrawLine(new Vector2(LeftMargin, warnYoff), new Vector2(LeftMargin + width, warnYoff), Color.DarkGray.WithAlpha(0.8f));

            // mid payload line
            var midYoff = height - midPayload;
            handle.DrawLine(new Vector2(LeftMargin, midYoff), new Vector2(LeftMargin + width, midYoff), Color.DarkGray.WithAlpha(0.8f));

            var minYoff = height - minPayload;
            handle.DrawLine(new Vector2(LeftMargin, minYoff), new Vector2(LeftMargin + width, minYoff), Color.DarkGray.WithAlpha(0.8f));

            // payload text
            handle.DrawString(_font, new Vector2(LeftMargin + width, extremeYoff), "128Kbit/s");
            handle.DrawString(_font, new Vector2(LeftMargin + width, warnYoff), "56Kbit/s");
            handle.DrawString(_font, new Vector2(LeftMargin + width, midYoff), "33.6Kbit/s");
            handle.DrawString(_font, new Vector2(LeftMargin + width, minYoff), "8.19Kbit/s");

            // avg text
            var lineHeight = _font.GetLineHeight(1) / 2f;
            handle.DrawString(_font, avgEnd - new Vector2(lineHeight, lineHeight), "average");

            // lag text info
            if(lastLagY != -1)
                handle.DrawString(_font, new Vector2(LeftMargin + width, lastLagY), $"{lastLagMs.ToString()}ms");

            // buffer text
            handle.DrawString(_font, new Vector2(LeftMargin, height + LowerGraphOffset), $"{_gameStateManager.GetApplicableStateCount().ToString()} states");
        }

        protected override void DisposeBehavior()
        {
            _gameStateManager.GameStateApplied -= HandleGameStateApplied;

            base.DisposeBehavior();
        }

        private sealed class NetShowGraphCommand : LocalizedCommands
        {
            public override string Command => "net_graph";

            public override void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                var overlayMan = IoCManager.Resolve<IOverlayManager>();

                if(!overlayMan.HasOverlay(typeof(NetGraphOverlay)))
                {
                    overlayMan.AddOverlay(new NetGraphOverlay());
                    shell.WriteLine("Enabled network overlay.");
                }
                else
                {
                    overlayMan.RemoveOverlay(typeof(NetGraphOverlay));
                    shell.WriteLine("Disabled network overlay.");
                }
            }
        }

        private sealed class NetWatchEntCommand : LocalizedCommands
        {
            [Dependency] private readonly IEntityManager _entManager = default!;
            [Dependency] private readonly IOverlayManager _overlayManager = default!;
            [Dependency] private readonly IPlayerManager _playerManager = default!;

            public override string Command => "net_watchent";

            public override void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                EntityUid? entity;

                if (args.Length == 0)
                {
                    entity = _playerManager.LocalEntity ?? EntityUid.Invalid;
                }
                else if (!NetEntity.TryParse(args[0], out var netEntity) || !_entManager.TryGetEntity(netEntity, out entity))
                {
                    shell.WriteError("Invalid argument: Needs to be 0 or an entityId.");
                    return;
                }

                if (!_overlayManager.TryGetOverlay(out NetGraphOverlay? overlay))
                {
                    overlay = new NetGraphOverlay();
                    _overlayManager.AddOverlay(overlay);
                }

                overlay.WatchEntId = entity.Value;
            }
        }
    }
}
