using Robust.Shared.Console;

namespace Robust.Client.Interfaces.Console
{
    public interface IConsoleCommand : Shared.Console.IConsoleCommand
    {
        /// <summary>
        /// Executes the command
        /// </summary>
        /// <returns>Whether or not the command should also be forwarded to the server. True to allow forwarding, false to block.</returns>
        bool Execute(IDebugConsole console, params string[] args);
    }
}
