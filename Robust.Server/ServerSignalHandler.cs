using Robust.Server.Interfaces;
using Robust.Shared;
using Robust.Shared.IoC;

namespace Robust.Server
{
    internal sealed class ServerSignalHandler : SignalHandler
    {
#pragma warning disable 649
        [Dependency] private readonly IBaseServer _baseServer;
#pragma warning restore 649

        protected override void OnReceiveTerminationSignal(string signal)
        {
            _baseServer.Shutdown($"{signal} received");
        }
    }
}
