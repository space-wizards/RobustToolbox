using System;
using Robust.Client.GameStates;
using Robust.Client.Profiling;
using Robust.Client.Timing;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Client.UserInterface.CustomControls.DebugMonitorControls
{
    internal sealed partial class DebugMonitors : BoxContainer, IDebugMonitors
    {
        [Dependency] private IClientGameTiming _timing = default!;
        [Dependency] private IClientGameStateManager _state = default!;
        [Dependency] private IConfigurationManager _cfg = default!;
        [Dependency] private IClientNetManager _net = default!;

        private readonly Control[] _monitors = new Control[Enum.GetNames<DebugMonitor>().Length];

        public void Init()
        {
            Visible = false;
            SeparationOverride = 2;
            Orientation = LayoutOrientation.Vertical;

            Add(DebugMonitor.Fps, new FpsCounter(_timing));
            Add(DebugMonitor.Coords, new DebugCoordsPanel());
            Add(DebugMonitor.Net, new DebugNetPanel(_net, _timing));
            Add(DebugMonitor.Bandwidth, new DebugNetBandwidthPanel(_net, _timing));
            Add(DebugMonitor.Time, new DebugTimePanel(_timing, _state));
            Add(DebugMonitor.Frames, new FrameGraph(_timing, _cfg));
            Add(DebugMonitor.Memory, new DebugMemoryPanel());
            Add(DebugMonitor.Clyde, new DebugClydePanel { HorizontalAlignment = HAlignment.Left });
            Add(DebugMonitor.System, new DebugSystemPanel { HorizontalAlignment = HAlignment.Left });
            Add(DebugMonitor.Version, new DebugVersionPanel(_cfg) { HorizontalAlignment = HAlignment.Left });
            Add(DebugMonitor.Input, new DebugInputPanel { HorizontalAlignment = HAlignment.Left });
            Add(DebugMonitor.Prof, new LiveProfileViewControl());

            void Add(DebugMonitor monitor, Control instance)
            {
                _monitors[(int)monitor] = instance;
                AddChild(instance);
            }
        }

        public void ToggleMonitor(DebugMonitor monitor)
        {
            _monitors[(int)monitor].Visible ^= true;
        }

        public void SetMonitor(DebugMonitor monitor, bool visible)
        {
            _monitors[(int)monitor].Visible = visible;
        }
    }
}
