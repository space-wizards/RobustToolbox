using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.State;
using SS14.Client.UserInterface.Controls;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Player;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timing;

namespace SS14.Client.UserInterface.CustomControls
{
    public class DebugMonitors : VBoxContainer, IDebugMonitors
    {
        public bool ShowFPS { get => _fpsCounter.Visible; set => _fpsCounter.Visible = value; }
        public bool ShowCoords { get => _debugCoordsPanel.Visible; set => _debugCoordsPanel.Visible = value; }
        public bool ShowNet { get => _debugNetPanel.Visible; set => _debugNetPanel.Visible = value; }
        public bool ShowTime { get => _timeDebug.Visible; set => _timeDebug.Visible = value; }
        public bool ShowFrameGraph { get => _frameGraph.Visible; set => _frameGraph.Visible = value; }

        private FPSCounter _fpsCounter;
        private DebugCoordsPanel _debugCoordsPanel;
        private DebugNetPanel _debugNetPanel;
        private DebugTimePanel _timeDebug;
        private FrameGraph _frameGraph;

        private readonly IGameTiming _gameTiming;
        private readonly IPlayerManager _playerManager;
        private readonly IEyeManager _eyeManager;
        private readonly IInputManager _inputManager;
        private readonly IResourceCache _resourceCache;
        private readonly IStateManager _stateManager;
        private readonly IDisplayManager _displayManager;
        private readonly IClientNetManager _netManager;

        //TODO: Think about a factory for this
        public DebugMonitors(IGameTiming gameTiming, IPlayerManager playerManager, IEyeManager eyeManager, IInputManager inputManager, IResourceCache resourceCache, IStateManager stateManager, IDisplayManager displayManager, IClientNetManager netManager)
        {
            _gameTiming = gameTiming;
            _playerManager = playerManager;
            _eyeManager = eyeManager;
            _inputManager = inputManager;
            _resourceCache = resourceCache;
            _stateManager = stateManager;
            _displayManager = displayManager;
            _netManager = netManager;

            PerformLayout();
        }

        protected override void Initialize()
        {
            base.Initialize();
            MouseFilter = MouseFilterMode.Ignore;
            Visible = false;

            SetAnchorPreset(LayoutPreset.Wide);

            MarginLeft = 2;
            MarginTop = 2;
        }

        private void PerformLayout()
        {
            _fpsCounter = new FPSCounter(_gameTiming);
            AddChild(_fpsCounter);

            _debugCoordsPanel = new DebugCoordsPanel(_playerManager, _eyeManager, _inputManager,
                _resourceCache, _stateManager, _displayManager);
            AddChild(_debugCoordsPanel);

            _debugNetPanel = new DebugNetPanel(_netManager, _gameTiming, _resourceCache);
            AddChild(_debugNetPanel);

            _timeDebug = new DebugTimePanel(_resourceCache, _gameTiming)
            {
                Visible = false,
            };
            AddChild(_timeDebug);

            _frameGraph = new FrameGraph(_gameTiming);
            AddChild(_frameGraph);
        }
    }
}
