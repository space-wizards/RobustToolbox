using System;
using System.Diagnostics;
using SS14.Server;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Maps;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Console;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;

namespace Sandbox.Server
{
    /// <inheritdoc />
    public class EntryPoint : GameServer
    {
        private IBaseServer _server;

        /// <inheritdoc />
        public override void Init()
        {
            base.Init();

            _server = IoCManager.Resolve<IBaseServer>();

            _server.RunLevelChanged += HandleRunLevelChanged;

            _server.PlayerJoinedServer += HandlePlayerJoinedServer;
            _server.PlayerJoinedLobby += HandlePlayerJoinedLobby;
            _server.PlayerJoinedGame += HandlePlayerJoinedGame;
            _server.PlayerLeaveServer += HandlePlayerLeaveServer;
        }
        
        /// <inheritdoc />
        public override void Dispose()
        {
            _server.RunLevelChanged -= HandleRunLevelChanged;

            _server.PlayerJoinedServer -= HandlePlayerJoinedServer;
            _server.PlayerJoinedLobby -= HandlePlayerJoinedLobby;
            _server.PlayerJoinedGame -= HandlePlayerJoinedGame;
            _server.PlayerLeaveServer -= HandlePlayerLeaveServer;

            base.Dispose();
        }

        private void HandleRunLevelChanged(object sender, RunLevelChangedEventArgs args)
        {
            if (args.NewLevel == ServerRunLevel.PreGame)
            {
                IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Round loaded!");
            }
            else if (args.NewLevel == ServerRunLevel.Game)
            {
                IoCManager.Resolve<IPlayerManager>().SendJoinGameToAll();
                IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Round started!");
            }
            else if (args.NewLevel == ServerRunLevel.PostGame)
            {
                IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Round over!");
            }
            
        }

        private void HandlePlayerJoinedServer(object sender, PlayerEventArgs args)
        {
            args.Session.JoinLobby();
            IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player joined server!", args.Session.Index);
        }

        private void HandlePlayerJoinedLobby(object sender, PlayerEventArgs args)
        {
            IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player joined Lobby!", args.Session.Index);
        }

        private void HandlePlayerJoinedGame(object sender, PlayerEventArgs args)
        {
            IoCManager.Resolve<IPlayerManager>().SpawnPlayerMob(args.Session);
            IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player joined Game!", args.Session.Index);
        }

        private void HandlePlayerLeaveServer(object sender, PlayerEventArgs args)
        {
            IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player left!", args.Session.Index);
        }

        private void LoadVerse(string versePath)
        {
            var defManager = IoCManager.Resolve<ITileDefinitionManager>();
            var mapMgr = IoCManager.Resolve<IMapManager>();

            if(string.IsNullOrWhiteSpace(versePath))
            {
                NewDefaultMap(mapMgr, defManager, 1);
            }
            else
            {
                IoCManager.Resolve<IMapLoader>().Load(_server.MapName, mapMgr.GetMap(1));
            }
        }

        //TODO: This whole method should be removed once file loading/saving works, and replaced with a 'Demo' map.
        /// <summary>
        ///     Generates 'Demo' grid and inserts it into the map manager.
        /// </summary>
        /// <param name="mapManager">The map manager to work with.</param>
        /// <param name="defManager">The definition manager to work with.</param>
        /// <param name="gridID">The ID of the grid to generate and insert into the map manager.</param>
        private static void NewDefaultMap(IMapManager mapManager, ITileDefinitionManager defManager, int gridID)
        {
            mapManager.SuppressOnTileChanged = true;
            try
            {
                Logger.Log("Cannot find map. Generating blank map.", LogLevel.Warning);
                var floor = defManager["Floor"].TileId;

                Debug.Assert(floor > 0);

                var map = mapManager.CreateMap(1); //TODO: default map
                var grid = map.CreateGrid(1); //TODO: huh wha maybe? check grid ID

                for (var y = -32; y <= 32; ++y)
                {
                    for (var x = -32; x <= 32; ++x)
                    {
                        grid.SetTile(new LocalCoordinates(x, y, gridID, 1), new Tile(floor)); //TODO: Fix this
                    }
                }
            }
            finally
            {
                mapManager.SuppressOnTileChanged = false;
            }
        }
    }
}
