using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Server.ClientConsoleHost.Commands
{
    internal class TeleportCommand : IClientCommand
    {
        public string Command => "tp";
        public string Description => "Teleports a player to any location in the round.";
        public string Help => "tp <x> <y> [<mapID>]";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            if (player.Status != SessionStatus.InGame || player.attachedEntity == null)
                return;

            if (args.Length < 2 || !float.TryParse(args[0], out var posX) || !float.TryParse(args[1], out var posY))
                return;

            var mapMgr = IoCManager.Resolve<IMapManager>();

            var position = new Vector2(posX, posY);
            var entity = player.attachedEntity;
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
}
