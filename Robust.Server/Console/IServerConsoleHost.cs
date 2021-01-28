using Robust.Shared.Console;

namespace Robust.Server.Console
{
    /// <summary>
    /// The server console shell that executes commands.
    /// </summary>
    public interface IServerConsoleHost : IConsoleHost
    {
        /// <summary>
        /// Initializes the ConsoleShell service.
        /// </summary>
        void Initialize();
    }
}
