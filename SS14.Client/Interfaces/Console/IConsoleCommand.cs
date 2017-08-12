using SS14.Shared.Command;
using SS14.Shared.IoC;

namespace SS14.Client.Interfaces.Console
{
    public interface IConsoleCommand : ICommand
    {
        /// <summary>
        /// Executes the command
        /// </summary>
        /// <returns>Whether or not the command should also be forwarded to the server. True to allow forwarding, false to block.</returns>
        bool Execute(IDebugConsole console, params string[] args);
    }
}
