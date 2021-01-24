using System.Collections.Generic;
using Robust.Shared.Players;

namespace Robust.Shared.Console
{
    public interface IConsoleHost
    {
        /// <summary>
        /// A map of (commandName -> ICommand) of every registered command in the shell.
        /// </summary>
        IReadOnlyDictionary<string, IConsoleCommand> AvailableCommands { get; }

        /// <summary>
        /// The local shell of the peer that is always available.
        /// </summary>
        IConsoleShell LocalShell { get; }

        //TODO: shared ConCmd Registration

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

        void WriteLine(ICommonSession? session, string text);
    }
}
