using SS14.Server.Interfaces.Commands;
using System.Collections.Generic;

namespace SS14.Server.Interfaces.ServerConsole
{
    public interface IConsoleManager
    {
        IDictionary<string, IConsoleCommand> AvailableCommands { get; }
        void Update();
    }
}
