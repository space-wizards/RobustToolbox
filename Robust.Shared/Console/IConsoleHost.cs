using System;
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

    public delegate void ConAnyCommandCallback(IConsoleShell shell, string commandName, string argStr, string[] args);

    /// <summary>
    /// The console host exists as a singleton subsystem that provides all of the features of the console API.
    /// It will register console commands, spawn console shells and execute command strings.
    /// </summary>
    public interface IConsoleHost
    {
        /// <summary>
        /// Is the shell running on the client?
        /// </summary>
        bool IsClient => !IsServer;

        /// <summary>
        /// Is the shell running on the server?
        /// </summary>
        bool IsServer { get; }

        /// <summary>
        /// The local shell of the peer that is always available.
        /// </summary>
        IConsoleShell LocalShell { get; }

        /// <summary>
        /// A map of (commandName -> ICommand) of every registered command in the shell.
        /// </summary>
        IReadOnlyDictionary<string, IConsoleCommand> RegisteredCommands { get; }

        /// <summary>
        /// Invoked before any console command is executed.
        /// </summary>
        event ConAnyCommandCallback AnyCommandExecuted;
        event EventHandler ClearText;

        /// <summary>
        /// Scans all loaded assemblies for console commands and registers them. This will NOT sync with connected clients, and
        /// should only be used during server initialization.
        /// </summary>
        void LoadConsoleCommands();

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
        /// Unregisters a console command that has been registered previously with <see cref="RegisterCommand"/>.
        /// If the specified command was registered automatically or isn't registered at all, the method will throw.
        /// </summary>
        /// <param name="command">The string identifier for the command.</param>
        void UnregisterCommand(string command);

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
        /// Execute a command string on the local shell.
        /// </summary>
        /// <param name="command">Command string to execute.</param>
        void ExecuteCommand(string command);

        /// <summary>
        /// Executes a command string on this specific session shell. If the command does not exist, the command will be forwarded
        /// to the
        /// remote shell.
        /// </summary>
        /// <param name="session">Session of the client to execute the command.</param>
        /// <param name="command">command line string to execute.</param>
        void ExecuteCommand(ICommonSession? session, string command);

        /// <summary>
        /// Executes the command string on the remote peer. This is mainly used to forward commands from the client to the server.
        /// If there is no remote peer (this is a local shell), this function does nothing.
        /// </summary>
        /// <param name="session">Session of the remote peer to execute the command on.</param>
        /// <param name="command">Command line string to execute at the remote endpoint.</param>
        void RemoteExecuteCommand(ICommonSession? session, string command);

        /// <summary>
        /// Sends a text string to the remote session.
        /// </summary>
        /// <param name="session">
        /// Remote session to send the text message to. If this is null, the text is sent to the local
        /// console.
        /// </param>
        /// <param name="text">Text message to send.</param>
        void WriteLine(ICommonSession? session, string text);

        /// <summary>
        /// Sends a foreground colored text string to the remote session.
        /// </summary>
        /// <param name="session">
        /// Remote session to send the text message to. If this is null, the text is sent to the local
        /// console.
        /// </param>
        /// <param name="text">Text message to send.</param>
        void WriteError(ICommonSession? session, string text);

        /// <summary>
        /// Removes all text from the local console.
        /// </summary>
        void ClearLocalConsole();
    }
}
