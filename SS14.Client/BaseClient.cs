using System;
using System.Diagnostics;
using Lidgren.Network;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.State;
using SS14.Client.Player;
using SS14.Client.State.States;
using SS14.Shared;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Players;

namespace SS14.Client
{
    public class BaseClient : IBaseClient
    {
        [Dependency]
        private readonly IClientNetManager _net;

        [Dependency]
        private readonly IPlayerManager _playMan;

        /// <inheritdoc />
        public ushort DefaultPort { get; } = 1212;

        /// <inheritdoc />
        public ClientRunLevel RunLevel { get; private set; }

        /// <inheritdoc />
        public ServerInfo GameInfo { get; private set; }

        /// <inheritdoc />
        public void Initialize()
        {
            _net.RegisterNetMessage<MsgServerInfo>(MsgServerInfo.NAME, (int) MsgServerInfo.ID, HandleServerInfo);

            _net.Connected += OnConnected;
            _net.ConnectFailed += OnConnectFailed;
            _net.Disconnect += OnNetDisconnect;

            _playMan.Initialize();

            Reset();
        }

        /// <inheritdoc />
        public void Update() { }

        /// <inheritdoc />
        public void Tick() { }

        /// <inheritdoc />
        public void Dispose() { }

        /// <inheritdoc />
        public void ConnectToServer(string ip, ushort port)
        {
            Debug.Assert(RunLevel < ClientRunLevel.Connecting);
            Debug.Assert(!_net.IsConnected);

            _net.Startup();

            OnRunLevelChanged(ClientRunLevel.Connecting);
            _net.ClientConnect(ip, port);
        }

        /// <inheritdoc />
        public void DisconnectFromServer(string reason)
        {
            Debug.Assert(RunLevel > ClientRunLevel.Initialize);
            Debug.Assert(_net.IsConnected);

            // runlevel changed in OnNetDisconnect()
            // are both of these *really* needed?
            _net.ClientDisconnect(reason);
        }

        /// <inheritdoc />
        public event EventHandler<RunLevelChangedEvent> RunLevelChanged;

        private void OnConnected(object sender, NetChannelArgs args)
        {
            // request base info about the server
            var msgInfo = _net.CreateNetMessage<MsgServerInfoReq>();
            msgInfo.PlayerName = "Joe Hello";
            _net.ClientSendMessage(msgInfo, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        ///     Player session is fully built, player is an active member of the server. Player is prepaired to start
        ///     receiving states when they join the lobby.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        private void PlayerJoinedServer(PlayerSession session)
        {
            Debug.Assert(RunLevel < ClientRunLevel.Connected);
            OnRunLevelChanged(ClientRunLevel.Connected);

            //TODO: Notify Content
        }

        /// <summary>
        ///     Player is joining the lobby for whatever reason.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        private void PlayerJoinedLobby(PlayerSession session)
        {
            Debug.Assert(RunLevel >= ClientRunLevel.Connected);
            OnRunLevelChanged(ClientRunLevel.Lobby);

            //TODO: Notify Content
        }

        /// <summary>
        ///     Player is joining the game (usually from lobby.)
        /// </summary>
        /// <param name="session">Session of the player.</param>
        private void PlayerJoinedGame(PlayerSession session)
        {
            Debug.Assert(RunLevel >= ClientRunLevel.Lobby);
            OnRunLevelChanged(ClientRunLevel.Ingame);

            //TODO: Notify Content
        }

        private void Reset()
        {
            OnRunLevelChanged(ClientRunLevel.Initialize);
        }

        private void OnConnectFailed(object sender, NetConnectFailArgs args)
        {
            Debug.Assert(RunLevel == ClientRunLevel.Connecting);
            Reset();
        }

        private void OnNetDisconnect(object sender, NetChannelArgs args)
        {
            Debug.Assert(RunLevel > ClientRunLevel.Initialize);

            _playMan.Shutdown();

            Reset();
        }

        private void HandleServerInfo(NetMessage message)
        {
            if (GameInfo == null)
                GameInfo = new ServerInfo();

            var msg = (MsgServerInfo) message;
            var info = GameInfo;

            info.ServerName = msg.ServerName;
            info.ServerPort = msg.ServerPort;
            info.ServerWelcomeMessage = msg.ServerWelcomeMessage;
            info.ServerMaxPlayers = msg.ServerMaxPlayers;
            info.ServerMapName = msg.ServerMapName;
            info.GameMode = msg.GameMode;
            info.ServerPlayerCount = msg.ServerPlayerCount;
            info.Index = msg.PlayerIndex;

            // start up player management
            _playMan.Startup(_net.ServerChannel);

            _playMan.LocalPlayer.Index = info.Index;

            _playMan.LocalPlayer.StatusChanged += OnLocalStatusChanged;
        }

        private void OnLocalStatusChanged(object obj, StatusEventArgs eventArgs)
        {
            // player finished fully connecting to the server.
            if (eventArgs.OldStatus == SessionStatus.Connected)
                PlayerJoinedServer(_playMan.LocalPlayer.Session);

            if (eventArgs.NewStatus == SessionStatus.InLobby)
            {
                var stateMan = IoCManager.Resolve<IStateManager>();
                stateMan.RequestStateChange<Lobby>();
                PlayerJoinedLobby(_playMan.LocalPlayer.Session);
            }

            if (eventArgs.NewStatus == SessionStatus.InGame)
            {
                var stateMan = IoCManager.Resolve<IStateManager>();
                stateMan.RequestStateChange<GameScreen>();
                PlayerJoinedGame(_playMan.LocalPlayer.Session);
            }
        }

        private void OnRunLevelChanged(ClientRunLevel newRunLevel)
        {
            Logger.Debug($"[ENG] Runlevel changed to: {newRunLevel}");
            var evnt = new RunLevelChangedEvent(RunLevel, newRunLevel);
            RunLevel = newRunLevel;
            RunLevelChanged?.Invoke(this, evnt);
        }
    }

    public enum ClientRunLevel
    {
        Error = 0,
        Initialize,
        Connecting,
        Connected,
        Lobby,
        Ingame,
        ChangeLevel
    }

    public class RunLevelChangedEvent : EventArgs
    {
        public ClientRunLevel OldLevel { get; }
        public ClientRunLevel NewLevel { get; }

        public RunLevelChangedEvent(ClientRunLevel oldLevel, ClientRunLevel newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }

    public class ServerInfo
    {
        public string ServerName { get; set; }
        public int ServerPort { get; set; }
        public string ServerWelcomeMessage { get; set; }
        public int ServerMaxPlayers { get; set; }
        public string ServerMapName { get; set; }
        public string GameMode { get; set; }
        public int ServerPlayerCount { get; set; }
        public PlayerIndex Index { get; set; }
    }
}
