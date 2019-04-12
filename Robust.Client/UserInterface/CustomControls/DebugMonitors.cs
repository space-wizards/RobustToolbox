using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Reflection;

namespace Robust.Client.UserInterface.CustomControls
{
    public class DebugMonitors : VBoxContainer, IDebugMonitors
    {
        public bool ShowFPS { get => FPSCounter.Visible; set => FPSCounter.Visible = value; }
        public bool ShowCoords { get => DebugCoordsPanel.Visible; set => DebugCoordsPanel.Visible = value; }
        public bool ShowNet { get => _debugNetPanel.Visible; set => _debugNetPanel.Visible = value; }
        public bool ShowTime { get => _timeDebug.Visible; set => _timeDebug.Visible = value; }
        public bool ShowFrameGraph { get => _frameGraph.Visible; set => _frameGraph.Visible = value; }

        private FPSCounter FPSCounter;
        private DebugCoordsPanel DebugCoordsPanel;
        private DebugNetPanel _debugNetPanel;
        private DebugTimePanel _timeDebug;
        private FrameGraph _frameGraph;

        protected override void Initialize()
        {
            base.Initialize();
            MouseFilter = MouseFilterMode.Ignore;
            Visible = false;

            SetAnchorPreset(LayoutPreset.Wide);

            MarginLeft = 2;
            MarginTop = 2;

            FPSCounter = new FPSCounter();
            AddChild(FPSCounter);

            DebugCoordsPanel = new DebugCoordsPanel();
            AddChild(DebugCoordsPanel);

            _debugNetPanel = new DebugNetPanel();
            AddChild(_debugNetPanel);

            _timeDebug = new DebugTimePanel
            {
                Visible = false,
            };
            AddChild(_timeDebug);

            _frameGraph = new FrameGraph();
            AddChild(_frameGraph);
        }
    }
}
