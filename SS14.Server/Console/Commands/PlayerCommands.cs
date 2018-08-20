using System;
using System.Text;
using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Network;
using SS14.Shared.Players;

namespace SS14.Server.Console.Commands
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
            var transform = entity.GetComponent<ITransformComponent>();

            transform.DetachParent();

            IMapGrid grid;
            if (args.Length == 3 && int.TryParse(args[2], out var mapId) && mapMgr.TryGetMap(new MapId(mapId), out var map))
                grid = map.FindGridAt(position);
            else
                grid = transform.LocalPosition.Map.FindGridAt(position);

            transform.LocalPosition = new GridLocalCoordinates(position, grid);

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
            sb.AppendLine($"{"Player Name",20}{"IP Address",16}{"Status",12}{"Playing Time",14}{"Ping",9}");
            sb.AppendLine("-----------------------------------------------------------------");

            foreach (IPlayerSession p in players)
            {
                sb.Append($"  {p.Name,20}");
                sb.AppendLine(string.Format("  {0,21}{1,12}{2,14}{3,9}",
                    p.ConnectedClient.RemoteEndPoint,
                    p.Status.ToString(),
                    (DateTime.Now - p.ConnectedTime).ToString(@"hh\:mm\:ss"),
                    p.ConnectedClient.Ping + "ms"));
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
