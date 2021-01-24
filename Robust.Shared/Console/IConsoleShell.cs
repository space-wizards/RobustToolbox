using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Robust.Shared.Console
{
    /// <summary>
    /// The console shell that executes commands. Each shell executes commands in the context of a player
    /// session, or without a session in a local context.
    /// </summary>
    public interface IConsoleShell
    {
        /// <summary>
        /// The console host that owns this shell.
        /// </summary>
        IConsoleHost ConsoleHost { get; }

        /// <summary>
        /// Is the shell running on the client?
        /// </summary>
        bool IsClient => !IsServer;

        /// <summary>
        /// Is the shell running in a local context (no remote peer session)? If true, <see cref="Player" /> will be null.
        /// </summary>
        bool IsLocal => Player is not null;

        /// <summary>
        /// Is the shell running on the server?
        /// </summary>
        bool IsServer { get; }

        /// <summary>
        /// The remote peer that owns this shell. This is null if the shell is running local (<see cref="IsLocal" /> is true.).
        /// </summary>
        ICommonSession? Player { get; }

        /// <summary>
        /// A map of (commandName -> ICommand) of every registered command in the shell.
        /// </summary>
        IReadOnlyDictionary<string, IConsoleCommand> RegisteredCommands { get; }

        /// <summary>
        /// Executes a command string on this specific session shell. If the command does not exist, the command will be forwarded to the
        /// remote shell.
        /// </summary>
        /// <param name="command">command line string to execute.</param>
        void ExecuteCommand(string command);

        /// <summary>
        /// Executes the command string on the remote peer. This is mainly used to forward commands from the client to the server.
        /// If there is no remote peer (this is a local shell), this function does nothing.
        /// </summary>
        /// <param name="command">Command line string to execute at the remote endpoint.</param>
        void RemoteExecuteCommand(string command);

        /// <summary>
        /// Writes a line to the output of the console.
        /// </summary>
        /// <param name="text">Line of text to write.</param>
        void WriteLine(string text);

        /// <summary>
        /// Write a line with a specific color to the console window.
        /// </summary>
        /// <param name="text">Line of text to write.</param>
        /// <param name="color">Foreground color of the string of text.</param>
        void WriteLine(string text, Color color);

        /// <summary>
        /// Clears the entire console of text.
        /// </summary>
        void Clear();
    }
}
