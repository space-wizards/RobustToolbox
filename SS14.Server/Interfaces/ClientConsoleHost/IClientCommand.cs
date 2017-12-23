using SS14.Shared.Console;
using SS14.Shared.Interfaces.Network;

namespace SS14.Server.Interfaces.ClientConsoleHost
{
    /// <summary>
    /// A command, executed from the debug console of a client.
    /// </summary>
    public interface IClientCommand : ICommand
    {
        void Execute(IClientConsoleHost host, INetChannel client, params string[] args);
    }
}
