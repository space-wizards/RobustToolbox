using SS14.Shared.Command;
using System.Collections.Generic;

namespace SS14.Server.Interfaces.ClientConsoleHost
{
    /// <summary>
    /// A command, executed from the debug console of a client.
    /// </summary>
    public interface IClientCommand : ICommand
    {
        void Execute(IClientConsoleHost host, IClient client, params string[] args);

        //void Register(Dictionary<string, IClientCommand> commands);
    }
}
