using System.Collections.Generic;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.Network;

namespace SS14.Server.Interfaces.Console
{
    /// <summary>
    /// The server console shell that executes commands.
    /// </summary>
    public interface IConsoleShell
    {
        /// <summary>
        /// A map of (commandName -> ICommand) of every registered command in the shell.
        /// </summary>
        IReadOnlyDictionary<string, IClientCommand> AvailableCommands { get; }

        /// <summary>
        /// Initializes the ConsoleShell service.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Scans all loaded assemblies for console commands and registers them. This will NOT sync with connected clients, and
        ///     should only be used during server initialization.
        /// </summary>
        void ReloadCommands();

        /// <summary>
        /// Sends a text string to the remote player.
        /// </summary>
        /// <param name="session">Remote player to send the text message to. If this is null, the text is sent to the local console.</param>
        /// <param name="text">Text message to send.</param>
        void SendText(IPlayerSession session, string text);

        /// <summary>
        /// Sends a text string to the remote console.
        /// </summary>
        /// <param name="target">Net channel to send the text string to.</param>
        /// <param name="text">Text message to send.</param>
        void SendText(INetChannel target, string text);

        /// <summary>
        /// Execute a command string on the local shell.
        /// </summary>
        /// <param name="command">Command string to execute.</param>
        void ExecuteCommand(string command);

        /// <summary>
        /// Execute a command string as a player.
        /// </summary>
        /// <param name="player">Session of the remote player. If this is null, the command is executed as the local console.</param>
        /// <param name="command">Command string to execute.</param>
        void ExecuteCommand(IPlayerSession player, string command);

        /// <summary>
        /// Elevates a player shell from user group to administrator group.
        /// </summary>
        /// <param name="session">Session shell to elevate.</param>
        /// <param name="password">super user password.</param>
        /// <returns></returns>
        bool ElevateShell(IPlayerSession session, string password);
    }
}
