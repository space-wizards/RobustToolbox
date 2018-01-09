using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;

namespace SS14.Server.ClientConsoleHost.Commands
{
    class AddMap : IClientCommand
    {
        public string Command => "addmap";
        public string Description => "Adds a new map to the round. If the mapID already exists, this command does nothing.";
        public string Help => "addmap <mapID>";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            if (args.Length < 1)
                return;

            var mapId = int.Parse(args[0]);

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
}
