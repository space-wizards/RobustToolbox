using Robust.Client.Interfaces;
using Robust.Shared;
using Robust.Shared.IoC;

namespace Robust.Client
{
    internal sealed class ClientSignalHandler : SignalHandler
    {
#pragma warning disable 649
        [Dependency] private readonly IGameController _gameController;
#pragma warning restore 649

        protected override void OnReceiveTerminationSignal(string signal)
        {
            _gameController.Shutdown($"{signal} received");
        }
    }
}
