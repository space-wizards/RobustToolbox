using SS14.Shared.Command;

namespace SS14.Server.Services.ServerConsole
{
    public abstract class ConsoleCommand : ICommand
    {
        public abstract string Command { get; }
        public abstract string Description { get; }
        public abstract string Help { get; }

        public abstract void Execute(params string[] args);
    }
}