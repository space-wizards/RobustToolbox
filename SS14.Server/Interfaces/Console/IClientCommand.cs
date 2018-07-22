using SS14.Server.Interfaces.Player;
using SS14.Shared.Console;

namespace SS14.Server.Interfaces.Console
{
    /// <summary>
    ///     A command, executed from the debug console of a client.
    /// </summary>
    public interface IClientCommand : ICommand
    {
        /// <summary>
        /// Executes the client command.
        /// </summary>
        /// <param name="shell">The console that executed this command.</param>
        /// <param name="player">The player that ran this command. This is null if the command was ran by the server console.</param>
        /// <param name="args">An array of all the parsed arguments.</param>
        void Execute(IConsoleShell shell, IPlayerSession player, string[] args);
    }
}
