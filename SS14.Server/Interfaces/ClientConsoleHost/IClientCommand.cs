using SS14.Server.Interfaces.Player;
using SS14.Shared.Console;

namespace SS14.Server.Interfaces.ClientConsoleHost
{
    /// <summary>
    ///     A command, executed from the debug console of a client.
    /// </summary>
    public interface IClientCommand : ICommand
    {
        void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args);
    }
}
