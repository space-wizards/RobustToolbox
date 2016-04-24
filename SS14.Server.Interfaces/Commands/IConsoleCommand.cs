using SS14.Shared.Command;

namespace SS14.Server.Interfaces.Commands
{
    public interface IConsoleCommand : ICommand
    {
        void Execute(params string[] args);
    }
}