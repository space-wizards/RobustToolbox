using System;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.Profiling;
using Robust.Client.State;
using Robust.Client.Timing;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    internal sealed class DebugMonitors : BoxContainer, IDebugMonitors
    {
        private readonly Control[] _monitors = new Control[Enum.GetNames<DebugMonitor>().Length];

        //TODO: Think about a factory for this
        public DebugMonitors(IClientGameTiming gameTiming, IPlayerManager playerManager, IEyeManager eyeManager,
            IInputManager inputManager, IStateManager stateManager, IClyde displayManager, IClientNetManager netManager,
            IMapManager mapManager)
        {
            Visible = false;

            Orientation = LayoutOrientation.Vertical;

            Add(DebugMonitor.Fps, new FpsCounter(gameTiming));
            Add(DebugMonitor.Coords, new DebugCoordsPanel());
            Add(DebugMonitor.Net, new DebugNetPanel(netManager, gameTiming));
            Add(DebugMonitor.Bandwidth, new DebugNetBandwidthPanel(netManager, gameTiming));
            Add(DebugMonitor.Time, new DebugTimePanel(gameTiming, IoCManager.Resolve<IClientGameStateManager>()));
            Add(DebugMonitor.Frames, new FrameGraph(gameTiming, IoCManager.Resolve<IConfigurationManager>()));
            Add(DebugMonitor.Memory, new DebugMemoryPanel());
            Add(DebugMonitor.Clyde, new DebugClydePanel { HorizontalAlignment = HAlignment.Left });
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
