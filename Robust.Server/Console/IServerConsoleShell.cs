using System.Collections.Generic;
using Robust.Shared.Console;

namespace Robust.Server.Console
{
    /// <summary>
    /// The server console shell that executes commands.
    /// </summary>
    public interface IServerConsoleShell : IConsoleShell
    {
        /// <summary>
        /// A map of (commandName -> ICommand) of every registered command in the shell.
        /// </summary>
        IReadOnlyDictionary<string, IServerCommand> RegisteredCommands { get; }
    }
}
