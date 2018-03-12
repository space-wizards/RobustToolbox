using System;
using System.Diagnostics;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.State;
using SS14.Client.Player;
using SS14.Client.State.States;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Players;

namespace SS14.Client
{
    /// <inheritdoc />
    public class BaseClient : IBaseClient
    {
        [Dependency]
        private readonly IClientNetManager _net;

        [Dependency]
        private readonly IPlayerManager _playMan;

        [Dependency]
        private readonly IStateManager _stateManager;

        /// <inheritdoc />
        public ushort DefaultPort { get; } = 1212;

        /// <inheritdoc />
        public ClientRunLevel RunLevel { get; private set; }

        /// <inheritdoc />
        public ServerInfo GameInfo { get; private set; }

        /// <inheritdoc />
        public void Initialize()
        {
            _net.RegisterNetMessage<MsgServerInfo>(MsgServerInfo.NAME, HandleServerInfo);

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

            // run level changed in OnNetDisconnect()
            // are both of these *really* needed?
            _net.ClientDisconnect(reason);
        }

        /// <inheritdoc />
        public event EventHandler<RunLevelChangedEventArgs> RunLevelChanged;

        public event EventHandler<PlayerEventArgs> PlayerJoinedServer;
        public event EventHandler<PlayerEventArgs> PlayerJoinedLobby;
        public event EventHandler<PlayerEventArgs> PlayerJoinedGame;
        public event EventHandler<PlayerEventArgs> PlayerLeaveServer;

        private void OnConnected(object sender, NetChannelArgs args)
        {
            // request base info about the server
            var msgInfo = _net.CreateNetMessage<MsgServerInfoReq>();
            msgInfo.PlayerName = "Joe Hello";
            _net.ClientSendMessage(msgInfo);
        }

        /// <summary>
        ///     Player session is fully built, player is an active member of the server. Player is prepared to start
        ///     receiving states when they join the lobby.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        private void OnPlayerJoinedServer(PlayerSession session)
        {
            Debug.Assert(RunLevel < ClientRunLevel.Connected);
            OnRunLevelChanged(ClientRunLevel.Connected);

            PlayerJoinedServer?.Invoke(this, new PlayerEventArgs(session));
        }

        /// <summary>
        ///     Player is joining the lobby for whatever reason.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        private void OnPlayerJoinedLobby(PlayerSession session)
        {
            Debug.Assert(RunLevel >= ClientRunLevel.Connected);
            OnRunLevelChanged(ClientRunLevel.Lobby);

            PlayerJoinedLobby?.Invoke(this, new PlayerEventArgs(session));
        }

        /// <summary>
        ///     Player is joining the game (usually from lobby.)
        /// </summary>
        /// <param name="session">Session of the player.</param>
        private void OnPlayerJoinedGame(PlayerSession session)
        {
            Debug.Assert(RunLevel >= ClientRunLevel.Lobby);
            OnRunLevelChanged(ClientRunLevel.Ingame);

            PlayerJoinedGame?.Invoke(this, new PlayerEventArgs(session));
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

            PlayerLeaveServer?.Invoke(this, new PlayerEventArgs(_playMan.LocalPlayer.Session));

            _stateManager.RequestStateChange<MainScreen>();

            _playMan.Shutdown();
            Reset();
        }

        private void HandleServerInfo(NetMessage message)
        {
            if (GameInfo == null)
                GameInfo = new ServerInfo();

            var msg = (MsgServerInfo)message;
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
            if (eventArgs.OldStatus == SessionStatus.Connecting)
                OnPlayerJoinedServer(_playMan.LocalPlayer.Session);

            if (eventArgs.NewStatus == SessionStatus.InLobby)
            {
                _stateManager.RequestStateChange<Lobby>();
                OnPlayerJoinedLobby(_playMan.LocalPlayer.Session);
            }

            else if (eventArgs.NewStatus == SessionStatus.InGame)
            {
                _stateManager.RequestStateChange<GameScreen>();

                OnPlayerJoinedGame(_playMan.LocalPlayer.Session);

                // request entire map be sent to us
                var mapMsg = _net.CreateNetMessage<MsgMapReq>();
                _net.ClientSendMessage(mapMsg);
            }
        }

        private void OnRunLevelChanged(ClientRunLevel newRunLevel)
        {
            Logger.Debug($"[ENG] Runlevel changed to: {newRunLevel}");
            var args = new RunLevelChangedEventArgs(RunLevel, newRunLevel);
            RunLevel = newRunLevel;
            RunLevelChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    ///     Enumeration of the run levels of the BaseClient.
    /// </summary>
    public enum ClientRunLevel
    {
        Error = 0,
        Initialize,
        Connecting,
        Connected,
        Lobby,
        Ingame,
        ChangeMap
    }

    /// <summary>
    ///     Event arguments for when something changed with the player.
    /// </summary>
    public class PlayerEventArgs : EventArgs
    {
        /// <summary>
        ///     The session that triggered the event.
        /// </summary>
        private PlayerSession Session { get; }

        /// <summary>
        ///     Constructs a new instance of the class.
        /// </summary>
        public PlayerEventArgs(PlayerSession session)
        {
            Session = session;
        }
    }

    /// <summary>
    ///     Event arguments for when the RunLevel has changed in the BaseClient.
    /// </summary>
    public class RunLevelChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     RunLevel that the BaseClient switched from.
        /// </summary>
        public ClientRunLevel OldLevel { get; }

        /// <summary>
        ///     RunLevel that the BaseClient switched to.
        /// </summary>
        public ClientRunLevel NewLevel { get; }

        /// <summary>
        ///     Constructs a new instance of the class.
        /// </summary>
        public RunLevelChangedEventArgs(ClientRunLevel oldLevel, ClientRunLevel newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }

    /// <summary>
    ///     Info about the server and player that is sent to the client while connecting.
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        ///     Current name of the server.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        ///     Current port the server is listening on.
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        ///     Current welcome message that is displayed when the client connects.
        /// </summary>
        public string ServerWelcomeMessage { get; set; }

        /// <summary>
        ///     Max number of players that are allowed in the server at one time.
        /// </summary>
        public int ServerMaxPlayers { get; set; }

        /// <summary>
        ///     Name of the current map loaded on the server.
        /// </summary>
        public string ServerMapName { get; set; }

        /// <summary>
        ///     Name of the current game mode loaded on the server.
        /// </summary>
        public string GameMode { get; set; }

        /// <summary>
        ///     Current number of players connected to the server, never greater than ServerMaxPlayers.
        /// </summary>
        public int ServerPlayerCount { get; set; }

        /// <summary>
        ///     Index of the client inside the PlayerManager.
        /// </summary>
        public PlayerIndex Index { get; set; }
    }
}
