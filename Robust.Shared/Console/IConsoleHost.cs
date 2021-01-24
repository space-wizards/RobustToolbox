using System.Collections.Generic;
using Robust.Shared.Players;

namespace Robust.Shared.Console
{
    /// <summary>
    /// A delegate that is called when the command is executed inside a shell.
    /// </summary>
    /// <param name="shell">The console shell that executed this command.</param>
    /// <param name="argStr">Unparsed text of the complete command with arguments.</param>
    /// <param name="args">An array of all the parsed arguments.</param>
    public delegate void ConCommandCallback(IConsoleShell shell, string argStr, string[] args);

    /// <summary>
    /// The console host exists as a singleton subsystem that provides all of the features of the console API.
    /// It will register console commands, spawn console shells and execute command strings.
    /// </summary>
    public interface IConsoleHost
    {
        /// <summary>
        /// The local shell of the peer that is always available.
        /// </summary>
        IConsoleShell LocalShell { get; }

        /// <summary>
        /// A map of (commandName -> ICommand) of every registered command in the shell.
        /// </summary>
        IReadOnlyDictionary<string, IConsoleCommand> RegisteredCommands { get; }

        /// <summary>
        /// Registers a console command into the console system. This is an alternative to
        /// creating an <see cref="IConsoleCommand"/> class.
        /// </summary>
        /// <param name="command">A string as identifier for this command.</param>
        /// <param name="description">Short one sentence description of the command.</param>
        /// <param name="help">Command format string.</param>
        /// <param name="callback"></param>
        void RegisterCommand(string command, string description, string help, ConCommandCallback callback);

        /// <summary>
        /// Returns the console shell for a given active session.
        /// </summary>
        /// <remarks>
        /// On the client this will always return the local shell, on the server this will return the shell of the active
        /// session.
        /// </remarks>
        /// <param name="session">Session to get the shell of.</param>
        /// <returns>Shell of the specified session.</returns>
        IConsoleShell GetSessionShell(ICommonSession session);

        /// <summary>
        /// Sends a text string to the remote session.
        /// </summary>
        /// <param name="session">
        /// Remote session to send the text message to. If this is null, the text is sent to the local
        /// console.
        /// </param>
        /// <param name="text">Text message to send.</param>
        void WriteLine(ICommonSession? session, string text);
    }
}
