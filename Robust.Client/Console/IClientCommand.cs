using Robust.Shared.Console;

namespace Robust.Client.Console
{
    public interface IClientCommand : IConsoleCommand
    {
        /// <summary>
        /// Executes the command
        /// </summary>
        /// <returns>Whether or not the command should also be forwarded to the server. True to allow forwarding, false to block.</returns>
        bool Execute(IClientConsoleShell shell, string argStr, string[] args);
    }
}
