using System;
using System.Diagnostics.CodeAnalysis;
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
    internal sealed class TeleportCommand : LocalizedCommands
    {
        [Dependency] private readonly IMapManager _map = default!;

        public override string Command => "tp";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.Status != SessionStatus.InGame)
                return;

            var transform = player.AttachedEntityTransform;
            if (transform == null)
                return;

            if (args.Length < 2 || !float.TryParse(args[0], out var posX) || !float.TryParse(args[1], out var posY))
            {
                shell.WriteError(Help);
                return;
            }

            var position = new Vector2(posX, posY);

            transform.AttachToGridOrMap();

            MapId mapId;
            if (args.Length == 3 && int.TryParse(args[2], out var intMapId))
                mapId = new MapId(intMapId);
            else
                mapId = transform.MapID;

            if (!_map.MapExists(mapId))
            {
                shell.WriteError($"Map {mapId} doesn't exist!");
                return;
            }

            if (_map.TryFindGridAt(mapId, position, out var grid))
            {
                var gridPos = grid.WorldToLocal(position);

                transform.Coordinates = new EntityCoordinates(grid.GridEntityId, gridPos);
            }
            else
            {
                var mapEnt = _map.GetMapEntityIdOrThrow(mapId);
                transform.WorldPosition = position;
                transform.AttachParent(mapEnt);
            }

            shell.WriteLine($"Teleported {player} to {mapId}:{posX},{posY}.");
        }
    }

    public sealed class TeleportToCommand : LocalizedCommands
    {
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Command => "tpto";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 0)
                return;

            var target = args[^1];

            if (!TryGetTransformFromUidOrUsername(target, shell, _entities, _players, out var targetTransform))
                return;

            var targetCoords = targetTransform.Coordinates;

            if (args.Length == 1)
            {
                var player = shell.Player as IPlayerSession;
                if (player?.Status != SessionStatus.InGame)
                {
                    shell.WriteError("You need to be in game to teleport to an entity.");
                    return;
                }

                if (!_entities.TryGetComponent(player.AttachedEntity, out TransformComponent? playerTransform))
                {
                    shell.WriteError("You don't have an entity.");
                    return;
                }

                playerTransform.Coordinates = targetCoords;
                playerTransform.AttachToGridOrMap();
            }
            else
            {
                foreach (var victim in args)
                {
                    if (victim == target)
                        continue;

                    if (!TryGetTransformFromUidOrUsername(victim, shell, _entities, _players, out var victimTransform))
                        return;

                    victimTransform.Coordinates = targetCoords;
                    victimTransform.AttachToGridOrMap();
                }
            }
        }

        private static bool TryGetTransformFromUidOrUsername(string str, IConsoleShell shell, IEntityManager entMan,
            IPlayerManager playerMan, [NotNullWhen(true)] out TransformComponent? transform)
        {
            if (int.TryParse(str, out var uid)
                && entMan.TryGetComponent(new EntityUid(uid), out transform))
                return true;

            if (playerMan.TryGetSessionByUsername(str, out var session)
                && entMan.TryGetComponent(session.AttachedEntity, out transform))
                return true;

            if (session == null)
                shell.WriteError("Can't find username/id: " + str);
            else
                shell.WriteError(str + " does not have an entity.");
            transform = null;
            return false;
        }
    }

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

            var players = _players.ServerSessions;
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

    internal sealed class KickCommand : LocalizedCommands
    {
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;

        public override string Command => "kick";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                var player = shell.Player as IPlayerSession;
                var toKickPlayer = player ?? _players.ServerSessions.FirstOrDefault();
                if (toKickPlayer == null)
                {
                    shell.WriteLine("You need to provide a player to kick.");
                    return;
                }
                shell.WriteLine($"You need to provide a player to kick. Try running 'kick {toKickPlayer?.Name}' as an example.");
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

                _netManager.DisconnectChannel(target.ConnectedClient, reason);
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = _players.ServerSessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

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
