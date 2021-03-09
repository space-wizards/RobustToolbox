using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    internal sealed class DebugMonitors : VBoxContainer, IDebugMonitors
    {
        public bool ShowFPS { get => _fpsCounter.Visible; set => _fpsCounter.Visible = value; }
        public bool ShowCoords { get => _debugCoordsPanel.Visible; set => _debugCoordsPanel.Visible = value; }
        public bool ShowNet { get => _debugNetPanel.Visible; set => _debugNetPanel.Visible = value; }
        public bool ShowNetBandwidth { get => _debugNetBandwidthPanel.Visible; set => _debugNetBandwidthPanel.Visible = value; }
        public bool ShowTime { get => _timeDebug.Visible; set => _timeDebug.Visible = value; }
        public bool ShowFrameGraph { get => _frameGraph.Visible; set => _frameGraph.Visible = value; }
        public bool ShowMemory { get => _debugMemoryPanel.Visible; set => _debugMemoryPanel.Visible = value; }
        public bool ShowClyde { get => _debugClydePanel.Visible; set => _debugClydePanel.Visible = value; }
        public bool ShowInput { get => _debugInputPanel.Visible; set => _debugInputPanel.Visible = value; }

        private readonly FpsCounter _fpsCounter;
        private readonly DebugCoordsPanel _debugCoordsPanel;
        private readonly DebugNetPanel _debugNetPanel;
        private readonly DebugTimePanel _timeDebug;
        private readonly FrameGraph _frameGraph;
        private readonly DebugMemoryPanel _debugMemoryPanel;
        private readonly DebugClydePanel _debugClydePanel;
        private readonly DebugInputPanel _debugInputPanel;
        private readonly DebugNetBandwidthPanel _debugNetBandwidthPanel;

        //TODO: Think about a factory for this
        public DebugMonitors(IGameTiming gameTiming, IPlayerManager playerManager, IEyeManager eyeManager, IInputManager inputManager, IStateManager stateManager, IClyde displayManager, IClientNetManager netManager, IMapManager mapManager)
        {
            var gameTiming1 = gameTiming;

            Visible = false;

            /*
            SetAnchorPreset(LayoutPreset.Wide);

            MarginLeft = 2;
            MarginTop = 2;
            */

            _fpsCounter = new FpsCounter(gameTiming1);
            AddChild(_fpsCounter);

            _debugCoordsPanel = new DebugCoordsPanel();
            AddChild(_debugCoordsPanel);

            _debugNetPanel = new DebugNetPanel(netManager, gameTiming1);
            AddChild(_debugNetPanel);

            _debugNetBandwidthPanel = new DebugNetBandwidthPanel(netManager, gameTiming1);
            AddChild(_debugNetBandwidthPanel);

            _timeDebug = new DebugTimePanel(gameTiming1, IoCManager.Resolve<IClientGameStateManager>());
            AddChild(_timeDebug);

            _frameGraph = new FrameGraph(gameTiming1);
            AddChild(_frameGraph);

            AddChild(_debugMemoryPanel = new DebugMemoryPanel());

            AddChild(_debugClydePanel = new DebugClydePanel
            {
                HorizontalAlignment = HAlignment.Left
            });

            AddChild(_debugInputPanel = new DebugInputPanel
            {
                HorizontalAlignment = HAlignment.Left
            });
        }
    }
}
