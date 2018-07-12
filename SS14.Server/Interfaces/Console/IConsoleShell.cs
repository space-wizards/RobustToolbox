using System.Collections.Generic;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network.Messages;

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

        void Initialize();
        void ProcessCommand(MsgConCmd message);
        void SendConsoleText(INetChannel target, string text);

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

        void ExecuteHostCommand(string command);
        void ExecuteCommand(IPlayerSession player, string command);
    }
}
