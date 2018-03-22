using System;
using System.Text;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Players;

namespace SS14.Server.ClientConsoleHost.Commands
{
    internal class TeleportCommand : IClientCommand
    {
        public string Command => "tp";
        public string Description => "Teleports a player to any location in the round.";
        public string Help => "tp <x> <y> [<mapID>]";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            if (player.Status != SessionStatus.InGame || player.AttachedEntity == null)
                return;

            if (args.Length < 2 || !float.TryParse(args[0], out var posX) || !float.TryParse(args[1], out var posY))
                return;

            var mapMgr = IoCManager.Resolve<IMapManager>();

            var position = new Vector2(posX, posY);
            var entity = player.AttachedEntity;
            var transform = entity.GetComponent<IServerTransformComponent>();

            transform.DetachParent();

            IMapGrid grid;
            if (args.Length == 3 && int.TryParse(args[2], out var mapId) && mapMgr.TryGetMap(new MapId(mapId), out var map))
                grid = map.FindGridAt(position);
            else
                grid = transform.LocalPosition.Map.FindGridAt(position);

            transform.LocalPosition = new LocalCoordinates(position, grid);

            host.SendConsoleReply(player.ConnectedClient, $"Teleported {player} to {grid.MapID}:{posX},{posY}.");
        }
    }

    public class ListPlayers : IClientCommand
    {
        public string Command => "listplayers";
        public string Description => "Lists all players currently connected";
        public string Help => "Usage: listplayers";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            var sb = new StringBuilder();

            var players = IoCManager.Resolve<IPlayerManager>().GetAllPlayers();
            sb.AppendLine("Current Players:");
            sb.AppendLine($"{"Index",1}{"Player Name",20}{"IP Address",16}{"Status",12}{"Playing Time",14}{"Ping",9}");

            foreach (IPlayerSession p in players)
            {
                sb.Append($"  {p.Index,3}");
                sb.Append($"  {p.Name,20}");
                sb.AppendLine(string.Format("  {0,16}{1,12}{2,14}{3,9}",
                    p.ConnectedClient.RemoteAddress,
                    p.Status.ToString(),
                    (DateTime.Now - p.ConnectedTime).ToString(@"hh\:mm\:ss"),
                    p.ConnectedClient.Ping + "ms"));
            }

            host.SendConsoleReply(player.ConnectedClient, sb.ToString());
        }
    }

    internal class KickCommand : IClientCommand
    {
        public string Command => "kick";
        public string Description => "Kicks a connected player out of the server, disconnecting them.";
        public string Help => "kick <PlayerIndex> [<Reason>]";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            if(args.Length < 1)
                return;

            if (int.TryParse(args[0], out var number))
            {
                var players = IoCManager.Resolve<IPlayerManager>();
                var index = new PlayerIndex(number);

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
}
