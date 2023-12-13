using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Shared.Console
{
    /// <inheritdoc />
    public sealed class ConsoleShell : IConsoleShell
    {
        /// <summary>
        /// Constructs a new instance of <see cref="ConsoleShell"/>.
        /// </summary>
        /// <param name="host">Console Host that owns this shell.</param>
        /// <param name="session">Player Session that this shell represents. May be null if this is a local server-side shell.</param>
        /// <param name="isLocal">Whether this is a local or remote shell.</param>
        public ConsoleShell(IConsoleHost host, ICommonSession? session, bool isLocal)
        {
            ConsoleHost = host;
            Player = session;
            IsLocal = isLocal;
        }

        /// <inheritdoc />
        public IConsoleHost ConsoleHost { get; }

        /// <inheritdoc />
        public bool IsServer => ConsoleHost.IsServer;

        /// <inheritdoc />
        public ICommonSession? Player { get; }

        /// <inheritdoc />
        public bool IsLocal { get; }

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

        public void WriteLine(FormattedMessage message)
        {
            ConsoleHost.WriteLine(Player, message);
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
