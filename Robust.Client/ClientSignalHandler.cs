using Robust.Client.Interfaces;
using Robust.Shared;
using Robust.Shared.IoC;

namespace Robust.Client
{
    internal sealed class ClientSignalHandler : SignalHandler
    {
        [Dependency] private readonly IGameController _gameController = default!;

        protected override void OnReceiveTerminationSignal(string signal)
        {
            _gameController.Shutdown($"{signal} received");
        }
    }
}
