using SS14.Server.Interfaces.Player;
using SS14.Shared.Console;

namespace SS14.Server.Interfaces.ServerConsole
{
    public interface IConsoleCommand : ICommand
    {
        void Execute(IConsoleManager host, IPlayerSession player, string[] args);
    }
}
