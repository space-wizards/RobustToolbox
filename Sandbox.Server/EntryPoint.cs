using System;
using SS14.Server;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Console;
using SS14.Shared.ContentPack;
using SS14.Shared.IoC;

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
            if (args.NewLevel == ServerRunLevel.Game)
            {
                IoCManager.Resolve<IPlayerManager>().SendJoinGameToAll();
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

        private void HandlePlayerJoinedLobby(object sender, PlayerEventArgs playerEventArgs)
        {

        }

        private void HandlePlayerJoinedGame(object sender, PlayerEventArgs args)
        {
            IoCManager.Resolve<IPlayerManager>().SpawnPlayerMob(args.Session);
        }

        private void HandlePlayerLeaveServer(object sender, PlayerEventArgs args)
        {
            IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player left!", args.Session.Index);
        }
    }
}
