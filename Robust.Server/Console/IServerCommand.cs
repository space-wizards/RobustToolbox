using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;

namespace Robust.Server.Console
{
    /// <summary>
    ///     A command, executed from the debug console of a client.
    /// </summary>
    public interface IServerCommand : IConsoleCommand
    {
        /// <summary>
        /// Executes the client command.
        /// </summary>
        /// <param name="shell">The console that executed this command.</param>
        /// <param name="args">An array of all the parsed arguments.</param>
        void Execute(IServerConsoleShell shell, string[] args);
    }
}
