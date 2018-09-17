using SS14.Client.UserInterface.Controls;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.Reflection;

namespace SS14.Client.UserInterface.CustomControls
{
    public class DebugMonitors : VBoxContainer, IDebugMonitors
    {
        public bool ShowFPS { get => FPSCounter.Visible; set => FPSCounter.Visible = value; }
        public bool ShowCoords { get => DebugCoordsPanel.Visible; set => DebugCoordsPanel.Visible = value; }
        public bool ShowNet { get => NetDebugPanel.Visible; set => NetDebugPanel.Visible = value; }
        public bool ShowTime { get => _timeDebug.Visible; set => _timeDebug.Visible = value; }

        private FPSCounter FPSCounter;
        private DebugCoordsPanel DebugCoordsPanel;
        private NetDebugPanel NetDebugPanel;
        private DebugTimePanel _timeDebug;

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

            NetDebugPanel = new NetDebugPanel();
            AddChild(NetDebugPanel);

            _timeDebug = new DebugTimePanel
            {
                Visible = false,
            };
            AddChild(_timeDebug);
        }
    }
}
