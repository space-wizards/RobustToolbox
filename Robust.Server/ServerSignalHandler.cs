using Robust.Server.Interfaces;
using Robust.Shared;
using Robust.Shared.IoC;

namespace Robust.Server
{
    internal sealed class ServerSignalHandler : SignalHandler
    {
        [Dependency] private readonly IBaseServer _baseServer = default!;

        protected override void OnReceiveTerminationSignal(string signal)
        {
            _baseServer.Shutdown($"{signal} received");
        }
    }
}
