using System;
using System.Diagnostics;
using SS14.Server;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Maps;
using SS14.Server.Interfaces.Player;
using SS14.Server.Player;
using SS14.Shared;
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
        private IPlayerManager _players;

        /// <inheritdoc />
        public override void Init()
        {
            base.Init();

            _server = IoCManager.Resolve<IBaseServer>();
            _players = IoCManager.Resolve<IPlayerManager>();

            _server.RunLevelChanged += HandleRunLevelChanged;
            _players.PlayerStatusChanged += HandlePlayerStatusChanged;

            _server.PlayerJoinedServer += HandlePlayerJoinedServer;
            _server.PlayerJoinedLobby += HandlePlayerJoinedLobby;
            _server.PlayerJoinedGame += HandlePlayerJoinedGame;
            _server.PlayerLeaveServer += HandlePlayerLeaveServer;
        }


        /// <inheritdoc />
        public override void Dispose()
        {
            _server.RunLevelChanged -= HandleRunLevelChanged;
            _players.PlayerStatusChanged -= HandlePlayerStatusChanged;

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
                IoCManager.Resolve<IPlayerManager>().FallbackSpawnPoint = new LocalCoordinates(0, 0, GridId.DefaultGrid, new MapId(1));
                NewDemoGrid(new GridId(1), new MapId(1));

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

        private void HandlePlayerStatusChanged(object sender, SessionStatusEventArgs args)
        {
            switch (args.NewStatus)
            {
                case SessionStatus.InLobby:
                {
                    // auto start game when first player joins
                    if (_server.RunLevel == ServerRunLevel.PreGame)
                        _server.RunLevel = ServerRunLevel.Game;

                    IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player joined Lobby!", args.Session.Index);
                }
                    break;
            }
        }

        private void HandlePlayerJoinedServer(object sender, PlayerEventArgs args)
        {
            args.Session.JoinLobby();
            IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player joined server!", args.Session.Index);
        }

        private void HandlePlayerJoinedLobby(object sender, PlayerEventArgs args)
        {
            // auto start game when first player joins
            if (_server.RunLevel == ServerRunLevel.PreGame)
                _server.RunLevel = ServerRunLevel.Game;

            IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player joined Lobby!", args.Session.Index);
        }

        private void HandlePlayerJoinedGame(object sender, PlayerEventArgs args)
        {
            //TODO: Check for existing mob and re-attach
            IoCManager.Resolve<IPlayerManager>().SpawnPlayerMob(args.Session);

            IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player joined Game!", args.Session.Index);
        }

        private void HandlePlayerLeaveServer(object sender, PlayerEventArgs args)
        {
            IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player left!", args.Session.Index);
        }

        //TODO: This whole method should be removed once file loading/saving works, and replaced with a 'Demo' map.
        /// <summary>
        ///     Generates 'Demo' grid and inserts it into the map manager.
        /// </summary>
        private void NewDemoGrid(GridId gridId, MapId mapId)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var defManager = IoCManager.Resolve<ITileDefinitionManager>();

            mapManager.SuppressOnTileChanged = true;

            Logger.Log("Cannot find map. Generating blank map.", LogLevel.Warning);
            var floor = defManager["Floor"].TileId;

            Debug.Assert(floor > 0);

            var map = mapManager.CreateMap(mapId);
            var grid = map.CreateGrid(gridId);

            for (var y = -32; y <= 32; ++y)
            {
                for (var x = -32; x <= 32; ++x)
                {
                    grid.SetTile(new LocalCoordinates(x, y, gridId, mapId), new Tile(floor));
                }
            }

            // load entities
            IoCManager.Resolve<IMapLoader>().Load(_server.MapName, map);

            mapManager.SuppressOnTileChanged = false;
        }
    }
}
