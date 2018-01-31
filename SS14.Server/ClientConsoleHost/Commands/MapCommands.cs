using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace SS14.Server.ClientConsoleHost.Commands
{
    class AddMapCommand : IClientCommand
    {
        public string Command => "addmap";
        public string Description => "Adds a new map to the round. If the mapID already exists, this command does nothing.";
        public string Help => "addmap <mapID>";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            if (args.Length < 1)
                return;

            var mapId = new MapId(int.Parse(args[0]));

            var mapMgr = IoCManager.Resolve<IMapManager>();

            if (!mapMgr.MapExists(mapId))
            {
                mapMgr.CreateMap(mapId);
                host.SendConsoleReply(player.ConnectedClient, $"Map with ID {mapId} created.");
                return;
            }

            host.SendConsoleReply(player.ConnectedClient, $"Map with ID {mapId} already exists!");
        }
    }

    class LocationCommand : IClientCommand
    {
        public string Command => "loc";
        public string Description => "Prints the absolute location of the player's entity to console.";
        public string Help => "loc";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            if(player.AttachedEntity == null)
                return;

            var pos = player.AttachedEntity.GetComponent<ITransformComponent>().LocalPosition;

            host.SendConsoleReply(player.ConnectedClient, $"MapID:{pos.MapID} GridID:{pos.GridID} X:{pos.X:N2} Y:{pos.Y:N2}");
        }
    }
}
