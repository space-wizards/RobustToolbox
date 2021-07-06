using System;
using System.Net;
using Robust.Client.Debugging;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Player;
using Robust.Client.Utility;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client
{
    /// <inheritdoc />
    public class BaseClient : IBaseClient
    {
        [Dependency] private readonly IClientNetManager _net = default!;
        [Dependency] private readonly IPlayerManager _playMan = default!;
        [Dependency] private readonly INetConfigurationManager _configManager = default!;
        [Dependency] private readonly IClientEntityManager _entityManager = default!;
        [Dependency] private readonly IEntityLookup _entityLookup = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IDiscordRichPresence _discord = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IClientGameStateManager _gameStates = default!;
        [Dependency] private readonly IDebugDrawingManager _debugDrawMan = default!;

        /// <inheritdoc />
        public ushort DefaultPort { get; } = 1212;

        /// <inheritdoc />
        public ClientRunLevel RunLevel { get; private set; }

        /// <inheritdoc />
        public ServerInfo? GameInfo { get; private set; }

        /// <inheritdoc />
        public string? PlayerNameOverride { get; set; }

        public string? LastDisconnectReason { get; private set; }

        /// <inheritdoc />
        public void Initialize()
        {
            _net.Connected += OnConnected;
            _net.ConnectFailed += OnConnectFailed;
            _net.Disconnect += OnNetDisconnect;

            _configManager.OnValueChanged(CVars.NetTickrate, TickRateChanged, invokeImmediately: true);

            _playMan.Initialize();
            _debugDrawMan.Initialize();
            Reset();
        }

        private void TickRateChanged(int tickrate)
        {
            if (GameInfo != null)
            {
                GameInfo.TickRate = (byte) tickrate;
            }

            _timing.TickRate = (byte) tickrate;
            Logger.InfoS("client", $"Tickrate changed to: {tickrate} on tick {_timing.CurTick}");
        }

        /// <inheritdoc />
        public void ConnectToServer(DnsEndPoint endPoint)
        {
            if (RunLevel == ClientRunLevel.Connecting)
            {
                _net.Shutdown("Client mashing that connect button.");
                Reset();
            }

            DebugTools.Assert(RunLevel < ClientRunLevel.Connecting);
            DebugTools.Assert(!_net.IsConnected);

            OnRunLevelChanged(ClientRunLevel.Connecting);
            _net.ClientConnect(endPoint.Host, endPoint.Port,
                PlayerNameOverride ?? _configManager.GetCVar(CVars.PlayerName));
        }

        /// <inheritdoc />
        public void DisconnectFromServer(string reason)
        {
            DebugTools.Assert(RunLevel > ClientRunLevel.Initialize);
            DebugTools.Assert(_net.IsConnected);
            // run level changed in OnNetDisconnect()
            // are both of these *really* needed?
            _net.ClientDisconnect(reason);
        }

        /// <inheritdoc />
        public void StartSinglePlayer()
        {
            DebugTools.Assert(RunLevel < ClientRunLevel.Connecting);
            DebugTools.Assert(!_net.IsConnected);
            _playMan.Startup();
            _playMan.LocalPlayer!.Name = PlayerNameOverride ?? _configManager.GetCVar(CVars.PlayerName);
            OnRunLevelChanged(ClientRunLevel.SinglePlayerGame);
            GameStartedSetup();
        }

        /// <inheritdoc />
        public void StopSinglePlayer()
        {
            DebugTools.Assert(RunLevel == ClientRunLevel.SinglePlayerGame);
            DebugTools.Assert(!_net.IsConnected);
            GameStoppedReset();
        }

        /// <inheritdoc />
        public event EventHandler<RunLevelChangedEventArgs>? RunLevelChanged;

        public event EventHandler<PlayerEventArgs>? PlayerJoinedServer;
        public event EventHandler<PlayerEventArgs>? PlayerJoinedGame;
        public event EventHandler<PlayerEventArgs>? PlayerLeaveServer;

        private void OnConnected(object? sender, NetChannelArgs args)
        {
            _configManager.SyncWithServer();
            _configManager.ReceivedInitialNwVars += OnReceivedClientData;
        }

        private void OnReceivedClientData(object? sender, EventArgs e)
        {
            _configManager.ReceivedInitialNwVars -= OnReceivedClientData;

            var info = GameInfo;

            var serverName = _configManager.GetCVar<string>("game.hostname");
            if (info == null)
            {
                GameInfo = info = new ServerInfo(serverName);
            }
            else
            {
                info.ServerName = serverName;
            }

            var maxPlayers = _configManager.GetCVar<int>("game.maxplayers");
            info.ServerMaxPlayers = maxPlayers;

            var userName = _net.ServerChannel!.UserName;
            var userId = _net.ServerChannel.UserId;
            _discord.Update(info.ServerName, userName, info.ServerMaxPlayers.ToString());
            // start up player management
            _playMan.Startup();

            _playMan.LocalPlayer!.UserId = userId;
            _playMan.LocalPlayer.Name = userName;

            _playMan.LocalPlayer.StatusChanged += OnLocalStatusChanged;
        }

        /// <summary>
        ///     Player session is fully built, player is an active member of the server. Player is prepared to start
        ///     receiving states when they join the lobby.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        private void OnPlayerJoinedServer(ICommonSession session)
        {
            DebugTools.Assert(RunLevel < ClientRunLevel.Connected);
            OnRunLevelChanged(ClientRunLevel.Connected);

            GameStartedSetup();

            PlayerJoinedServer?.Invoke(this, new PlayerEventArgs(session));
        }

        /// <summary>
        ///     Player is joining the game
        /// </summary>
        /// <param name="session">Session of the player.</param>
        private void OnPlayerJoinedGame(ICommonSession session)
        {
            DebugTools.Assert(RunLevel >= ClientRunLevel.Connected);
            OnRunLevelChanged(ClientRunLevel.InGame);

            PlayerJoinedGame?.Invoke(this, new PlayerEventArgs(session));
        }

        private void Reset()
        {
            _configManager.ClearReceivedInitialNwVars();
            OnRunLevelChanged(ClientRunLevel.Initialize);
        }

        private void OnConnectFailed(object? sender, NetConnectFailArgs args)
        {
            DebugTools.Assert(RunLevel == ClientRunLevel.Connecting);
            Reset();
        }

        private void OnNetDisconnect(object? sender, NetDisconnectedArgs args)
        {
            DebugTools.Assert(RunLevel > ClientRunLevel.Initialize);

            PlayerLeaveServer?.Invoke(this, new PlayerEventArgs(_playMan.LocalPlayer?.Session));

            LastDisconnectReason = args.Reason;
            GameStoppedReset();
        }

        private void GameStartedSetup()
        {
            _entityManager.Startup();
            _mapManager.Startup();
            _entityLookup.Startup();

            _timing.ResetSimTime();
            _timing.Paused = false;
        }

        private void GameStoppedReset()
        {
            IoCManager.Resolve<INetConfigurationManager>().FlushMessages();
            _gameStates.Reset();
            _playMan.Shutdown();
            _entityLookup.Shutdown();
            _entityManager.Shutdown();
            _mapManager.Shutdown();
            _discord.ClearPresence();
            Reset();
        }

        private void OnLocalStatusChanged(object? obj, StatusEventArgs eventArgs)
        {
            // player finished fully connecting to the server.
            // OldStatus is used here because it can go from connecting-> connected or connecting-> ingame
            if (eventArgs.OldStatus == SessionStatus.Connecting)
            {
                OnPlayerJoinedServer(_playMan.LocalPlayer!.Session);
            }

            if (eventArgs.NewStatus == SessionStatus.InGame)
            {
                OnPlayerJoinedGame(_playMan.LocalPlayer!.Session);
            }
        }

        private void OnRunLevelChanged(ClientRunLevel newRunLevel)
        {
            Logger.DebugS("client", $"Runlevel changed to: {newRunLevel}");
            var args = new RunLevelChangedEventArgs(RunLevel, newRunLevel);
            RunLevel = newRunLevel;
            RunLevelChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    ///     Enumeration of the run levels of the BaseClient.
    /// </summary>
    public enum ClientRunLevel : byte
    {
        Error = 0,

        /// <summary>
        ///     The client has not started connecting to a server (on main menu).
        /// </summary>
        Initialize,

        /// <summary>
        ///     The client started connecting to the server, and is in the process of building the session.
        /// </summary>
        Connecting,

        /// <summary>
        ///     The client has successfully finished connecting to the server.
        /// </summary>
        Connected,

        /// <summary>
        ///     The client is now in the game, moving around.
        /// </summary>
        InGame,

        /// <summary>
        ///     The client is now in singleplayer mode, in-game.
        /// </summary>
        SinglePlayerGame,
    }

    /// <summary>
    ///     Event arguments for when something changed with the player.
    /// </summary>
    public class PlayerEventArgs : EventArgs
    {
        /// <summary>
        ///     The session that triggered the event.
        /// </summary>
        private ICommonSession? Session { get; }

        /// <summary>
        ///     Constructs a new instance of the class.
        /// </summary>
        public PlayerEventArgs(ICommonSession? session)
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
        public ServerInfo(string serverName)
        {
            ServerName = serverName;
        }

        /// <summary>
        ///     Current name of the server.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        ///     Max number of players that are allowed in the server at one time.
        /// </summary>
        public int ServerMaxPlayers { get; set; }

        public byte TickRate { get; internal set; }
    }
}
