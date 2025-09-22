using System;
using System.Linq;
using System.Text;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Server.Console.Commands
{
    public sealed class ListPlayers : LocalizedCommands
    {
        [Dependency] private readonly IPlayerManager _players = default!;

        public override string Command => "listplayers";
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            // Player: number of people connected and their byond keys
            // Admin: read a byond variable which shows their ip, byond version, ckey, attached entity and hardware id
            // EDIT: inb4 sued by MSO for AGPLv3 violation.

            var sb = new StringBuilder();

            var players = _players.Sessions;
            sb.AppendLine($"{"Player Name",20} {"Status",12} {"Playing Time",14} {"Ping",9} {"IP EndPoint",20}");
            sb.AppendLine("-------------------------------------------------------------------------------");

            foreach (var p in players)
            {
                sb.AppendLine(string.Format("{4,20} {1,12} {2,14:hh\\:mm\\:ss} {3,9} {0,20}",
                    p.Channel.RemoteEndPoint,
                    p.Status.ToString(),
                    DateTime.UtcNow - p.ConnectedTime,
                    p.Channel.Ping + "ms",
                    p.Name));
            }

            shell.WriteLine(sb.ToString());
        }
    }

    internal sealed class KickCommand : LocalizedCommands
    {
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;

        public override string Command => "kick";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                var player = shell.Player;
                var toKickPlayer = player ?? _players.Sessions.FirstOrDefault();
                if (toKickPlayer == null)
                {
                    shell.WriteLine("You need to provide a player to kick.");
                    return;
                }
                shell.WriteLine($"You need to provide a player to kick. Try running 'kick {toKickPlayer.Name}' as an example.");
                return;
            }

            var name = args[0];

            if (_players.TryGetSessionByUsername(name, out var target))
            {
                string reason;
                if (args.Length >= 2)
                    reason = $"Kicked by console: {string.Join(' ', args[1..])}";
                else
                    reason = "Kicked by console";

                _netManager.DisconnectChannel(target.Channel, reason);
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = _players.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

                return CompletionResult.FromHintOptions(options, "<PlayerIndex>");
            }

            if (args.Length > 1)
            {
                return CompletionResult.FromHint("[<Reason>]");
            }

            return CompletionResult.Empty;
        }
    }
}
