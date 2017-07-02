using SS14.Shared.Command;
using SS14.Shared.IoC;
using System.Collections.Generic;
using SS14.Shared.Network;

namespace SS14.Server.Interfaces.ClientConsoleHost
{
    /// <summary>
    /// A command, executed from the debug console of a client.
    /// </summary>
    public interface IClientCommand : ICommand
    {
        void Execute(IClientConsoleHost host, NetChannel client, params string[] args);

        //void Register(Dictionary<string, IClientCommand> commands);
    }
}
