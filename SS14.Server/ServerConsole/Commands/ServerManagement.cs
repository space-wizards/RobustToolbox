using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Maps;
using SS14.Server.Interfaces.ServerConsole;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using System;
using System.IO;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;

namespace SS14.Server.ServerConsole.Commands
{
    public class RestartServer : IConsoleCommand
    {
        public string Command => "restart";
        public string Description => "Restarts the server";
        public string Help => "Restarts the server.";

        public void Execute(params string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Restart();
        }
    }

    // Crashes for some reason.
    public class StopServer : IConsoleCommand
    {
        public string Command => "shutdown";
        public string Description => "Stops the server";
        public string Help => "Stops the server brutally without telling clients.";

        public void Execute(params string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Shutdown();
        }
    }

    public class SaveMap : IConsoleCommand
    {
        public string Command => "save_map";
        public string Description => "Serializes the map to disk.";
        public string Help => "save_map <mapID>";

        public void Execute(params string[] args)
        {
            if (args.Length < 1)
                return;

            if (!int.TryParse(args[0], out var intGridId))
                return;

            var gridId = new GridId(intGridId);

            // no saving default grid
            if(gridId == GridId.DefaultGrid)
                return;

            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapID = new MapId(1);

            if(!mapManager.TryGetMap(mapID, out var map))
                return;

            if(!map.GridExists(gridId))
                return;

            IoCManager.Resolve<IMapLoader>().SaveBlueprint(map, gridId, "./Maps/Demo/FullGrid.yaml");
        }
    }
}
