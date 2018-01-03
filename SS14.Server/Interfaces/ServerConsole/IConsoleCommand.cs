using SS14.Shared.Console;

namespace SS14.Server.Interfaces.ServerConsole
{
    public interface IConsoleCommand : ICommand
    {
        void Execute(params string[] args);
    }
}
