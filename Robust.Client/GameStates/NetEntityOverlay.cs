using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Timing;
using Robust.Shared.Collections;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;

namespace Robust.Client.GameStates
{
    /// <summary>
    /// A network entity report that lists all entities as they are updated through game states.
    /// https://developer.valvesoftware.com/wiki/Networking_Entities#cl_entityreport
    /// </summary>
    sealed class NetEntityOverlay : Overlay
    {
        [Dependency] private readonly IClientGameTiming _gameTiming = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IClientGameStateManager _gameStateManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private const uint TrafficHistorySize = 64; // Size of the traffic history bar in game ticks.
        private const int _maxEnts = 128; // maximum number of entities to track.

        /// <inheritdoc />
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        private readonly Font _font;
        private readonly int _lineHeight;
        private readonly Dictionary<NetEntity, NetEntData> _netEnts = new();

        public NetEntityOverlay()
        {
            IoCManager.InjectDependencies(this);
            var cache = IoCManager.Resolve<IResourceCache>();
            _font = new VectorFont(cache.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"), 10);
            _lineHeight = _font.GetLineHeight(1);

            _gameStateManager.GameStateApplied += HandleGameStateApplied;
            _gameStateManager.PvsLeave += OnPvsLeave;
        }

        private void OnPvsLeave(MsgStateLeavePvs msg)
        {
            if (msg.Tick.Value + TrafficHistorySize < _gameTiming.LastRealTick.Value)
                return;

            foreach (var uid in msg.Entities)
            {
                if (!_netEnts.TryGetValue(uid, out var netEnt))
                    continue;

                if (netEnt.LastUpdate < msg.Tick)
                {
                    netEnt.InPVS = false;
                    netEnt.LastUpdate = msg.Tick;
                }

                netEnt.Traffic.Add(msg.Tick, NetEntData.EntState.PvsLeave);
            }
        }

        private void HandleGameStateApplied(GameStateAppliedArgs args)
        {
            var gameState = args.AppliedState;

            if (!gameState.EntityStates.HasContents)
                return;

            foreach (var entityState in gameState.EntityStates.Span)
            {
                if (!_netEnts.TryGetValue(entityState.NetEntity, out var netEnt))
                {
                    if (_netEnts.Count >= _maxEnts)
                        continue;

                    _netEnts[entityState.NetEntity] = netEnt = new();
                }

                if (!netEnt.InPVS && netEnt.LastUpdate < gameState.ToSequence)
                {
                    netEnt.InPVS = true;
                    netEnt.Traffic.Add(gameState.ToSequence, NetEntData.EntState.PvsEnter);
                }
                else
                    netEnt.Traffic.Add(gameState.ToSequence, NetEntData.EntState.Data);

                if (netEnt.LastUpdate < gameState.ToSequence)
                    netEnt.LastUpdate = gameState.ToSequence;

                //TODO: calculate size of state and record it here.
            }
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            if (!_netManager.IsConnected)
                return;

            switch (args.Space)
            {
                case OverlaySpace.ScreenSpace:
                    DrawScreen(args);
                    break;
            }
        }

        private void DrawScreen(in OverlayDrawArgs args)
        {
            // remember, 0,0 is top left of ui with +X right and +Y down
            var screenHandle = args.ScreenHandle;

            int i = 0;
            foreach (var (nent, netEnt) in _netEnts)
            {
                var uid = _entityManager.GetEntity(nent);

                if (!_entityManager.EntityExists(uid))
                {
                    _netEnts.Remove(nent);
                    continue;
                }

                var xPos = 100;
                var yPos = 10 + _lineHeight * i++;
                var name = $"({uid}) {_entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID}";
                var color = netEnt.TextColor(_gameTiming);
                screenHandle.DrawString(_font, new Vector2(xPos + (TrafficHistorySize + 4), yPos), name, color);
                DrawTrafficBox(screenHandle, netEnt, xPos, yPos);
            }
        }

        private void DrawTrafficBox(DrawingHandleScreen handle, NetEntData netEntity, int x, int y)
        {
            handle.DrawRect(UIBox2.FromDimensions(x + 1, y, TrafficHistorySize + 1, _lineHeight), new Color(32, 32, 32, 128));
            handle.DrawRect(UIBox2.FromDimensions(x, y, TrafficHistorySize + 2, _lineHeight), Color.Gray.WithAlpha(0.15f), false);

            var traffic = netEntity.Traffic;

            //TODO: Local peak size, actually scale the peaks
            for (uint i = 1; i <= TrafficHistorySize; i++)
            {
                if (!traffic.TryGetValue(_gameTiming.LastRealTick + (i - TrafficHistorySize), out var tickData))
                    continue;

                var color = tickData switch
                {
                    NetEntData.EntState.Data => Color.Green,
                    NetEntData.EntState.PvsLeave => Color.Orange,
                    NetEntData.EntState.PvsEnter => Color.Cyan,
                    _ => throw new Exception("Unexpected value")
                };

                var xPos = x + 1 + i;
                var yPosA = y + 1;
                var yPosB = yPosA + _lineHeight - 1;
                handle.DrawLine(new Vector2(xPos, yPosA), new Vector2(xPos, yPosB), color);
            }
        }

        protected override void DisposeBehavior()
        {
            _gameStateManager.GameStateApplied -= HandleGameStateApplied;
            _gameStateManager.PvsLeave -= OnPvsLeave;
            base.DisposeBehavior();
        }

        private sealed class NetEntData
        {
            public GameTick LastUpdate = GameTick.Zero;
            public readonly OverflowDictionary<GameTick, EntState> Traffic = new((int) TrafficHistorySize);
            public bool Exists = true;
            public bool InPVS = true;

            public Color TextColor(IClientGameTiming timing)
            {
                if (!InPVS)
                    return Color.Orange; // Entity still exists outside PVS, but not updated anymore.

                if (timing.LastRealTick < LastUpdate + timing.TickRate)
                    return Color.Blue; //Entity in PVS generating ongoing traffic.

                return Color.Green; // Entity in PVS, but not updated recently.
            }

            public enum EntState : byte
            {
                Nothing = 0,
                Data = 1,
                PvsLeave = 2,
                PvsEnter = 3
            }
        }

        private sealed class NetEntityReportCommand : LocalizedCommands
        {
            public override string Command => "net_entityreport";

            public override void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                var overlayMan = IoCManager.Resolve<IOverlayManager>();

                if (!overlayMan.HasOverlay(typeof(NetEntityOverlay)))
                {
                    overlayMan.AddOverlay(new NetEntityOverlay());
                    shell.WriteLine("Enabled network entity report overlay.");
                }
                else
                {
                    overlayMan.RemoveOverlay(typeof(NetEntityOverlay));
                    shell.WriteLine("Disabled network entity report overlay.");
                }
            }
        }
    }
}
