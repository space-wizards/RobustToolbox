using SS14.Shared.Command;
using System.Collections.Generic;

namespace SS14.Server.Interfaces.Commands
{
    public interface IClientCommand : ICommand
    {
        void Execute(IClient client, params string[] args);

        void Register(Dictionary<string, IClientCommand> commands);
    }
}