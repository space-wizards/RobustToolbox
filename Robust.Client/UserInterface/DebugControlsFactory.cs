using Robust.Client.Console;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Map;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface
{
    public interface IDebugControlsFactory
    {
        IDebugMonitors CreateMonitors();
        IDebugConsoleView CreateConsole();
    }

    public class ClientDebugControlsFactory : IDebugControlsFactory
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IClyde _displayManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IClientMapManager _mapManager = default!;

        public IDebugMonitors CreateMonitors()
        {
            return new DebugMonitors(_gameTiming, _playerManager, _eyeManager, _inputManager, _stateManager,
                _displayManager, _netManager, _mapManager);
        }

        public IDebugConsoleView CreateConsole()
        {
            return new DebugConsole(_consoleHost, _resourceManager);
        }
    }
}
