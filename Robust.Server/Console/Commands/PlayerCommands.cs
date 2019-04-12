using System;
using System.Text;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Players;

namespace Robust.Server.Console.Commands
{
    internal class TeleportCommand : IClientCommand
    {
        public string Command => "tp";
        public string Description => "Teleports a player to any location in the round.";
        public string Help => "tp <x> <y> [<mapID>]";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player.Status != SessionStatus.InGame || player.AttachedEntity == null)
                return;

            if (args.Length < 2 || !float.TryParse(args[0], out var posX) || !float.TryParse(args[1], out var posY))
                return;

            var mapMgr = IoCManager.Resolve<IMapManager>();

            var position = new Vector2(posX, posY);
            var entity = player.AttachedEntity;
            var transform = entity.Transform;

            transform.DetachParent();

            IMapGrid grid;
            if (args.Length == 3 && int.TryParse(args[2], out var mapId) && mapMgr.TryGetMap(new MapId(mapId), out var map))
                grid = map.FindGridAt(position);
            else
                grid = transform.GridPosition.Map.FindGridAt(position);

            transform.GridPosition = new GridCoordinates(position, grid);

            shell.SendText(player, $"Teleported {player} to {grid.MapID}:{posX},{posY}.");
        }
    }

    public class ListPlayers : IClientCommand
    {
        public string Command => "listplayers";
        public string Description => "Lists all players currently connected";
        public string Help => "listplayers";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            // Player: number of people connected and their byond keys
            // Admin: read a byond variable which shows their ip, byond version, ckey, attached entity and hardware id
            // EDIT: inb4 sued by MSO for AGPLv3 violation.

            var sb = new StringBuilder();

            var players = IoCManager.Resolve<IPlayerManager>().GetAllPlayers();
            sb.AppendLine($"{"Player Name",20} {"Status",12} {"Playing Time",14} {"Ping",9} {"IP EndPoint",20}");
            sb.AppendLine("-------------------------------------------------------------------------------");

            foreach (var p in players)
            {
                sb.AppendLine(string.Format("{4,20} {1,12} {2,14:hh\\:mm\\:ss} {3,9} {0,20}",
                    p.ConnectedClient.RemoteEndPoint,
                    p.Status.ToString(),
                    DateTime.Now - p.ConnectedTime,
                    p.ConnectedClient.Ping + "ms",
                    p.Name));
            }

            shell.SendText(player, sb.ToString());
        }
    }

    internal class KickCommand : IClientCommand
    {
        public string Command => "kick";
        public string Description => "Kicks a connected player out of the server, disconnecting them.";
        public string Help => "kick <PlayerIndex> [<Reason>]";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length < 1)
            {
                shell.SendText(player, $"You need to provide a player to kick. Try running 'kick {player.Name}' as an example.");
                return;
            }

            var name = args[0];

            var players = IoCManager.Resolve<IPlayerManager>();
            var index = new NetSessionId(name);

            if (players.ValidSessionId(index))
            {
                var network = IoCManager.Resolve<IServerNetManager>();
                var targetPlyr = players.GetSessionById(index);

                var reason = "Kicked by console.";
                if (args.Length >= 2)
                {
                    reason = reason + args[1];
                }

                network.DisconnectChannel(targetPlyr.ConnectedClient, reason);
            }
        }
    }
}
