using System;
using System.Linq;
using System.Text;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Robust.Server.Console.Commands
{
    internal class TeleportCommand : IConsoleCommand
    {
        public string Command => "tp";
        public string Description => "Teleports a player to any location in the round.";
        public string Help => "tp <x> <y> [<mapID>]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.Status != SessionStatus.InGame || player.AttachedEntity == null)
                return;

            if (args.Length < 2 || !float.TryParse(args[0], out var posX) || !float.TryParse(args[1], out var posY))
            {
                shell.WriteError(Help);
                return;
            }

            var mapMgr = IoCManager.Resolve<IMapManager>();

            var position = new Vector2(posX, posY);
            IEntity? tempQualifier = player.AttachedEntity;
            var transform = (tempQualifier != null ? IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(tempQualifier) : null);

            if(transform == null)
                return;

            transform.AttachToGridOrMap();

            MapId mapId;
            if (args.Length == 3 && int.TryParse(args[2], out var intMapId))
                mapId = new MapId(intMapId);
            else
                mapId = transform.MapID;

            if (!mapMgr.MapExists(mapId))
            {
                shell.WriteError($"Map {mapId} doesn't exist!");
                return;
            }

            if (mapMgr.TryFindGridAt(mapId, position, out var grid))
            {
                var gridPos = grid.WorldToLocal(position);

                transform.Coordinates = new EntityCoordinates(grid.GridEntityId, gridPos);
            }
            else
            {
                var mapEnt = mapMgr.GetMapEntity(mapId);
                transform.WorldPosition = position;
                transform.AttachParent(mapEnt);
            }

            shell.WriteLine($"Teleported {player} to {mapId}:{posX},{posY}.");
        }
    }

    public class TeleportToPlayerCommand : IConsoleCommand
    {
        public string Command => "tpto";
        public string Description => "Teleports the current player or the specified players to the location of last player specified.";
        public string Help => "tpto <username> [username]...";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.Status != SessionStatus.InGame || player.AttachedEntity == null)
                return;

            if (args.Length == 0)
            {
                shell.WriteError(Help);
            }
            else if (args.Length == 1)
            {
                var players = IoCManager.Resolve<IPlayerManager>();

                var username = args[0];

                if (!players.TryGetSessionByUsername(username, out var playerSession))
                {
                    shell.WriteError("Can't find username: " + username);
                    return;
                }

                if (playerSession.AttachedEntity == null)
                {
                    shell.WriteError(username + " does not have an entity.");
                    return;
                }

                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(player.AttachedEntity).Coordinates = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(playerSession.AttachedEntity).Coordinates;
            }
            else if (args.Length > 1)
            {
                var players = IoCManager.Resolve<IPlayerManager>();

                var target = args[^1];
                if (!players.TryGetSessionByUsername(target, out var targetSession))
                {
                    shell.WriteError("Can't find username: " + target);
                    return;
                }

                if (targetSession.AttachedEntity == null)
                {
                    shell.WriteError(target + " does not have an entity.");
                    return;
                }

                foreach (var username in args)
                {
                    if (username == target)
                        continue;

                    if (!players.TryGetSessionByUsername(username, out var playerSession))
                    {
                        shell.WriteError("Can't find username: " + username);
                        continue;
                    }

                    if (playerSession.AttachedEntity == null)
                    {
                        shell.WriteError(username + " does not have an entity.");
                        continue;
                    }

                    IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(playerSession.AttachedEntity).Coordinates = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(targetSession.AttachedEntity).Coordinates;
                }
            }
        }
    }

    public class ListPlayers : IConsoleCommand
    {
        public string Command => "listplayers";
        public string Description => "Lists all players currently connected";
        public string Help => "listplayers";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            // Player: number of people connected and their byond keys
            // Admin: read a byond variable which shows their ip, byond version, ckey, attached entity and hardware id
            // EDIT: inb4 sued by MSO for AGPLv3 violation.

            var sb = new StringBuilder();

            var players = IoCManager.Resolve<IPlayerManager>().ServerSessions;
            sb.AppendLine($"{"Player Name",20} {"Status",12} {"Playing Time",14} {"Ping",9} {"IP EndPoint",20}");
            sb.AppendLine("-------------------------------------------------------------------------------");

            foreach (var p in players)
            {
                sb.AppendLine(string.Format("{4,20} {1,12} {2,14:hh\\:mm\\:ss} {3,9} {0,20}",
                    p.ConnectedClient.RemoteEndPoint,
                    p.Status.ToString(),
                    DateTime.UtcNow - p.ConnectedTime,
                    p.ConnectedClient.Ping + "ms",
                    p.Name));
            }

            shell.WriteLine(sb.ToString());
        }
    }

    internal class KickCommand : IConsoleCommand
    {
        public string Command => "kick";
        public string Description => "Kicks a connected player out of the server, disconnecting them.";
        public string Help => "kick <PlayerIndex> [<Reason>]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var players = IoCManager.Resolve<IPlayerManager>();
            if (args.Length < 1)
            {
                var player = shell.Player as IPlayerSession;
                var toKickPlayer = player ?? players.ServerSessions.FirstOrDefault();
                if (toKickPlayer == null)
                {
                    shell.WriteLine("You need to provide a player to kick.");
                    return;
                }
                shell.WriteLine($"You need to provide a player to kick. Try running 'kick {toKickPlayer?.Name}' as an example.");
                return;
            }

            var name = args[0];

            if (players.TryGetSessionByUsername(name, out var target))
            {
                var network = IoCManager.Resolve<IServerNetManager>();

                string reason;
                if (args.Length >= 2)
                    reason = $"Kicked by console: {string.Join(' ', args[1..])}";
                else
                    reason = "Kicked by console";

                network.DisconnectChannel(target.ConnectedClient, reason);
            }
        }
    }
}
