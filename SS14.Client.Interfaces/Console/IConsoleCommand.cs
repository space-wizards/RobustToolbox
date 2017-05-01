using SS14.Shared.Command;

namespace SS14.Client.Interfaces.Console
{
    public interface IConsoleCommand : ICommand
    {
        void Execute(params string[] args);
    }
}
