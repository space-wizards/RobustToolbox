using System.Collections.Generic;
using SS14.Shared.IoC;

namespace SS14.Server.Interfaces.ServerConsole
{
    public interface IConsoleManager
    {
        IDictionary<string, IConsoleCommand> AvailableCommands { get; }
        void Update();
    }
}
