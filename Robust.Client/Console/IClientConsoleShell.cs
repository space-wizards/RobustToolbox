using System.Collections.Generic;
using Robust.Shared.Console;

namespace Robust.Client.Console
{
    public interface IClientConsoleShell : IConsoleShell
    {
        /// <summary>
        /// A map of (commandName -> ICommand) of every registered command in the shell.
        /// </summary>
        IReadOnlyDictionary<string, IClientCommand> RegisteredCommands { get; }
    }
}
