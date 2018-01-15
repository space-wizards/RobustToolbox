using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Maps;
using SS14.Server.Interfaces.ServerConsole;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using System;
using System.IO;
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
        public string Command => "savemap";
        public string Description => "Saves the map";
        public string Help => "Saves the map. A file path to save to can be provided, defaulting to Resources/SavedEntities.xml when not specified.";

        public void Execute(params string[] args)
        {
            var mapName = PathHelpers.ExecutableRelativeFile(Path.Combine("Resources", "SavedEntities.xml"));
            if (args.Length > 0)
            {
                mapName = args[0];
            }
            var mapManager = IoCManager.Resolve<IMapManager>();
            IoCManager.Resolve<IMapLoader>().Save(mapName, mapManager.GetMap(new MapId(1)));
        }
    }
}
