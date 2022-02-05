using Robust.Shared.Players;

namespace Robust.Shared.Console
{
    /// <inheritdoc />
    public sealed class ConsoleShell : IConsoleShell
    {
        /// <summary>
        /// Constructs a new instance of <see cref="ConsoleShell"/>.
        /// </summary>
        /// <param name="host">Console Host that owns this shell.</param>
        /// <param name="session">Player Session that this shell represents. If this is null, then
        /// the shell is representing the local console.</param>
        public ConsoleShell(IConsoleHost host, ICommonSession? session)
        {
            ConsoleHost = host;
            Player = session;
        }

        /// <inheritdoc />
        public IConsoleHost ConsoleHost { get; }

        /// <inheritdoc />
        public bool IsServer => ConsoleHost.IsServer;

        /// <inheritdoc />
        public ICommonSession? Player { get; }

        /// <inheritdoc />
        public void ExecuteCommand(string command)
        {
            ConsoleHost.ExecuteCommand(Player, command);
        }

        /// <inheritdoc />
        public void RemoteExecuteCommand(string command)
        {
            ConsoleHost.RemoteExecuteCommand(Player, command);
        }

        /// <inheritdoc />
        public void WriteLine(string text)
        {
            ConsoleHost.WriteLine(Player, text);
        }

        /// <inheritdoc />
        public void WriteError(string text)
        {
            ConsoleHost.WriteError(Player, text);
        }

        /// <inheritdoc />
        public void Clear()
        {
            // Only the local shell can clear the console
            if (Player is not null)
                return;

            ConsoleHost.ClearLocalConsole();
        }
    }
}
