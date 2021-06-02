using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates
{
    /// <summary>
    /// A network entity report that lists all entities as they are updated through game states.
    /// https://developer.valvesoftware.com/wiki/Networking_Entities#cl_entityreport
    /// </summary>
    class NetEntityOverlay : Overlay
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IClientGameStateManager _gameStateManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        private const int TrafficHistorySize = 64; // Size of the traffic history bar in game ticks.

        /// <inheritdoc />
        public override OverlaySpace Space => OverlaySpace.ScreenSpace | OverlaySpace.WorldSpace;

        private readonly Font _font;
        private readonly int _lineHeight;
        private readonly List<NetEntity> _netEnts = new();

        public NetEntityOverlay()
        {
            IoCManager.InjectDependencies(this);
            var cache = IoCManager.Resolve<IResourceCache>();
            _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
            _lineHeight = _font.GetLineHeight(1);

            _gameStateManager.GameStateApplied += HandleGameStateApplied;
        }

        private void HandleGameStateApplied(GameStateAppliedArgs args)
        {
            if(_gameTiming.InPrediction) // we only care about real server states.
                return;

            // Shift traffic history down one
            for (var i = 0; i < _netEnts.Count; i++)
            {
                var traffic = _netEnts[i].Traffic;
                for (int j = 1; j < TrafficHistorySize; j++)
                {
                    traffic[j - 1] = traffic[j];
                }

                traffic[^1] = 0;
            }

            var gameState = args.AppliedState;

            if(gameState.EntityStates is not null)
            {
                // Loop over every entity that gets updated this state and record the traffic
                foreach (var entityState in gameState.EntityStates)
                {
                    var newEnt = true;
                    for(var i=0;i<_netEnts.Count;i++)
                    {
                        var netEnt = _netEnts[i];

                        if (netEnt.Id != entityState.Uid)
                            continue;

                        //TODO: calculate size of state and record it here.
                        netEnt.Traffic[^1] = 1;
                        netEnt.LastUpdate = gameState.ToSequence;
                        newEnt = false;
                        _netEnts[i] = netEnt; // copy struct back
                        break;
                    }

                    if (!newEnt)
                        continue;

                    var newNetEnt = new NetEntity(entityState.Uid);
                    newNetEnt.Traffic[^1] = 1;
                    newNetEnt.LastUpdate = gameState.ToSequence;
                    _netEnts.Add(newNetEnt);
                }
            }

            bool pvsEnabled = _configurationManager.GetCVar<bool>("net.pvs");
            float pvsRange = _configurationManager.GetCVar<float>("net.maxupdaterange");
            var pvsCenter = _eyeManager.CurrentEye.Position;
            Box2 pvsBox = Box2.CenteredAround(pvsCenter.Position, new Vector2(pvsRange*2, pvsRange*2));

            int timeout = _gameTiming.TickRate * 3;
            for (int i = 0; i < _netEnts.Count; i++)
            {
                var netEnt = _netEnts[i];

                if(_entityManager.EntityExists(netEnt.Id))
                {
                    //TODO: Whoever is working on PVS remake, change the InPVS detection.
                    var position = _entityManager.GetEntity(netEnt.Id).Transform.MapPosition;
                    netEnt.InPVS =  !pvsEnabled || (pvsBox.Contains(position.Position) && position.MapId == pvsCenter.MapId);
                    _netEnts[i] = netEnt; // copy struct back
                    continue;
                }

                netEnt.Exists = false;
                if (netEnt.LastUpdate.Value + timeout < _gameTiming.LastRealTick.Value)
                {
                    _netEnts.RemoveAt(i);
                    i--;
                    continue;
                }

                _netEnts[i] = netEnt; // copy struct back
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
                case OverlaySpace.WorldSpace:
                    DrawWorld(args);
                    break;
            }
        }

        private void DrawWorld(in OverlayDrawArgs args)
        {
            bool pvsEnabled = _configurationManager.GetCVar<bool>("net.pvs");
            if(!pvsEnabled)
                return;

            float pvsRange = _configurationManager.GetCVar<float>("net.maxupdaterange");
            var pvsCenter = _eyeManager.CurrentEye.Position;
            Box2 pvsBox = Box2.CenteredAround(pvsCenter.Position, new Vector2(pvsRange * 2, pvsRange * 2));

            var worldHandle = args.WorldHandle;

            worldHandle.DrawRect(pvsBox, Color.Red, false);
        }

        private void DrawScreen(in OverlayDrawArgs args)
        {
            // remember, 0,0 is top left of ui with +X right and +Y down
            var screenHandle = args.ScreenHandle;

            for (int i = 0; i < _netEnts.Count; i++)
            {
                var netEnt = _netEnts[i];

                if (!_entityManager.TryGetEntity(netEnt.Id, out var ent))
                {
                    _netEnts.RemoveSwap(i);
                    i--;
                    continue;
                }

                var xPos = 100;
                var yPos = 10 + _lineHeight * i;
                var name = $"({netEnt.Id}) {ent.Prototype?.ID}";
                var color = CalcTextColor(ref netEnt);
                DrawString(screenHandle, _font, new Vector2(xPos + (TrafficHistorySize + 4), yPos), name, color);
                DrawTrafficBox(screenHandle, ref netEnt, xPos, yPos);
            }
        }

        private void DrawTrafficBox(DrawingHandleScreen handle, ref NetEntity netEntity, int x, int y)
        {
            handle.DrawRect(UIBox2.FromDimensions(x+1, y, TrafficHistorySize + 1, _lineHeight), new Color(32, 32, 32, 128));
            handle.DrawRect(UIBox2.FromDimensions(x, y, TrafficHistorySize + 2, _lineHeight), Color.Gray.WithAlpha(0.15f), false);

            var traffic = netEntity.Traffic;

            //TODO: Local peak size, actually scale the peaks
            for (int i = 0; i < TrafficHistorySize; i++)
            {
                if(traffic[i] == 0)
                    continue;

                var xPos = x + 1 + i;
                var yPosA = y + 1;
                var yPosB = yPosA + _lineHeight - 1;
                handle.DrawLine(new Vector2(xPos, yPosA), new Vector2(xPos, yPosB), Color.Green);
            }
        }

        private Color CalcTextColor(ref NetEntity ent)
        {
            if(!ent.Exists)
                return Color.Gray; // Entity is deleted, will be removed from list soon.

            if(!ent.InPVS)
                return Color.Red; // Entity still exists outside PVS, but not updated anymore.

            if(_gameTiming.LastRealTick < ent.LastUpdate + _gameTiming.TickRate)
                return Color.Blue; //Entity in PVS generating ongoing traffic.

            return Color.Green; // Entity in PVS, but not updated recently.
        }

        protected override void DisposeBehavior()
        {
            _gameStateManager.GameStateApplied -= HandleGameStateApplied;
            base.DisposeBehavior();
        }

        private static void DrawString(DrawingHandleScreen handle, Font font, Vector2 pos, string str, Color textColor)
        {
            var baseLine = new Vector2(pos.X, font.GetAscent(1) + pos.Y);

            foreach (var rune in str.EnumerateRunes())
            {
                var advance = font.DrawChar(handle, rune, baseLine, 1, textColor);
                baseLine += new Vector2(advance, 0);
            }
        }

        private struct NetEntity
        {
            public GameTick LastUpdate;
            public readonly EntityUid Id;
            public readonly int[] Traffic;
            public bool Exists;
            public bool InPVS;

            public NetEntity(EntityUid id)
            {
                LastUpdate = GameTick.Zero;
                Id = id;
                Traffic = new int[TrafficHistorySize];
                Exists = true;
                InPVS = true;
            }
        }

        private class NetEntityReportCommand : IConsoleCommand
        {
            public string Command => "net_entityreport";
            public string Help => "net_entityreport <0|1>";
            public string Description => "Toggles the net entity report panel.";

            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                if (args.Length != 1)
                {
                    shell.WriteError("Invalid argument amount. Expected 1 arguments.");
                    return;
                }

                if (!byte.TryParse(args[0], out var iValue))
                {
                    shell.WriteError("Invalid argument: Needs to be 0 or 1.");
                    return;
                }

                var bValue = iValue > 0;
                var overlayMan = IoCManager.Resolve<IOverlayManager>();

                if(bValue && !overlayMan.HasOverlay(typeof(NetEntityOverlay)))
                {
                    overlayMan.AddOverlay(new NetEntityOverlay());
                    shell.WriteLine("Enabled network entity report overlay.");
                }
                else if(!bValue && overlayMan.HasOverlay(typeof(NetEntityOverlay)))
                {
                    overlayMan.RemoveOverlay(typeof(NetEntityOverlay));
                    shell.WriteLine("Disabled network entity report overlay.");
                }
            }
        }
    }
}
