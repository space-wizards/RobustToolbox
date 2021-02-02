using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Robust.Shared.Console
{
    /// <inheritdoc />
    public class ConsoleShell : IConsoleShell
    {
        /// <inheritdoc />
        public IConsoleHost ConsoleHost { get; }

        /// <inheritdoc />
        public bool IsServer => ConsoleHost.IsServer;

        /// <inheritdoc />
        public ICommonSession? Player { get; }

        public ConsoleShell(IConsoleHost host, ICommonSession? session)
        {
            ConsoleHost = host;
            Player = session;
        }

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
        public void WriteLine(string text, Color color)
        {
            ConsoleHost.WriteLine(Player, text, color);
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
