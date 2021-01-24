using Robust.Shared.Console;
using Robust.Shared.Players;

namespace Robust.Server.Console
{
    /// <summary>
    /// The server console shell that executes commands.
    /// </summary>
    public interface IServerConsoleHost : IConsoleHost
    {
        /// <summary>
        /// Initializes the ConsoleShell service.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Scans all loaded assemblies for console commands and registers them. This will NOT sync with connected clients, and
        /// should only be used during server initialization.
        /// </summary>
        void ReloadCommands();

        /// <summary>
        /// Execute a command string on the local shell.
        /// </summary>
        /// <param name="command">Command string to execute.</param>
        void ExecuteCommand(string command);

        /// <summary>
        /// Execute a command string as a player.
        /// </summary>
        /// <param name="session">Session of the remote player. If this is null, the command is executed as the local console.</param>
        /// <param name="command">Command string to execute.</param>
        void ExecuteCommand(ICommonSession? session, string command);
    }
}
