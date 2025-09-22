using System;
using System.Net;
using Robust.Client.Configuration;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Player;
using Robust.Client.Utility;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client
{
    /// <inheritdoc />
    public sealed class BaseClient : IBaseClient, IPostInjectInit
    {
        [Dependency] private readonly IClientNetManager _net = default!;
        [Dependency] private readonly IPlayerManager _playMan = default!;
        [Dependency] private readonly IClientNetConfigurationManager _configManager = default!;
        [Dependency] private readonly IClientEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IDiscordRichPresence _discord = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IClientGameStateManager _gameStates = default!;
        [Dependency] private readonly ILogManager _logMan = default!;

        /// <inheritdoc />
        public ushort DefaultPort { get; } = 1212;

        /// <inheritdoc />
        public ClientRunLevel RunLevel { get; private set; }

        /// <inheritdoc />
        public ServerInfo? GameInfo { get; private set; }

        /// <inheritdoc />
        public string? PlayerNameOverride { get; set; }

        public string? LastDisconnectReason { get; private set; }

        private (TimeSpan, GameTick) _timeBase;
        private ISawmill _logger = default!;

        /// <inheritdoc />
        public void Initialize()
        {
            _net.Connected += OnConnected;
            _net.ConnectFailed += OnConnectFailed;
            _net.Disconnect += OnNetDisconnect;

            _net.RegisterNetMessage<MsgSyncTimeBase>(
                SyncTimeBase,
                NetMessageAccept.Handshake | NetMessageAccept.Client);

            _configManager.OnValueChanged(CVars.NetTickrate, TickRateChanged, invokeImmediately: true);

            _playMan.Initialize(0);
            _playMan.PlayerListUpdated += OnPlayerListUpdated;
            Reset();
        }

        private void OnPlayerListUpdated()
        {
            var serverPlayers = _playMan.PlayerCount;
            if (_net.ServerChannel != null && GameInfo != null && _net.IsConnected)
                _discord.Update(GameInfo.ServerName, _net.ServerChannel.UserName, GameInfo.ServerMaxPlayers.ToString(), serverPlayers.ToString());
        }

        private void SyncTimeBase(MsgSyncTimeBase message)
        {
            _logger.Debug($"Synchronized time base: {message.Tick}: {message.Time}");

            if (RunLevel >= ClientRunLevel.Connected)
                _timing.TimeBase = (message.Time, message.Tick);
            else
                _timeBase = (message.Time, message.Tick);
        }

        private void TickRateChanged(int tickrate, in CVarChangeInfo info)
        {
            if (GameInfo != null)
            {
                GameInfo.TickRate = (ushort) tickrate;
            }

            _timing.SetTickRateAt((ushort) tickrate, info.TickChanged);
            _logger.Info($"Tickrate changed to: {tickrate} on tick {_timing.CurTick}");
        }

        /// <inheritdoc />
        public void ConnectToServer(DnsEndPoint endPoint)
        {
            if (RunLevel == ClientRunLevel.Connecting)
            {
                _net.Reset("Client mashing that connect button.");
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
            _net.ClientDisconnect(reason);
        }

        /// <inheritdoc />
        public void StartSinglePlayer()
        {
            DebugTools.Assert(RunLevel < ClientRunLevel.Connecting);
            DebugTools.Assert(!_net.IsConnected);
            var name = PlayerNameOverride ?? _configManager.GetCVar(CVars.PlayerName);
            _playMan.SetupSinglePlayer(name);
            OnRunLevelChanged(ClientRunLevel.SinglePlayerGame);
            _playMan.JoinGame(_playMan.LocalSession!);
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

            var channel = _net.ServerChannel!;

            // start up player management
            _playMan.SetupMultiplayer(channel);
            _playMan.PlayerStatusChanged += OnStatusChanged;

            var serverPlayers = _playMan.PlayerCount;
            _discord.Update(info.ServerName, channel.UserName, info.ServerMaxPlayers.ToString(), serverPlayers.ToString());

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
            _configManager.ReceivedInitialNwVars -= OnReceivedClientData;
            _playMan.PlayerStatusChanged -= OnStatusChanged;
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

            // Don't invoke PlayerLeaveServer if PlayerJoinedServer & GameStartedSetup hasn't been called yet.
            if (RunLevel > ClientRunLevel.Connecting)
                PlayerLeaveServer?.Invoke(this, new PlayerEventArgs(_playMan.LocalSession));

            LastDisconnectReason = args.Reason;
            GameStoppedReset();
        }

        private void GameStartedSetup()
        {
            _entityManager.Startup();
            _mapManager.Startup();

            _timing.ResetSimTime(_timeBase);
            _timing.Paused = false;
        }

        private void GameStoppedReset()
        {
            _configManager.FlushMessages();
            _gameStates.Reset();
            _playMan.Shutdown();
            _entityManager.Shutdown();
            _mapManager.Shutdown();
            _discord.ClearPresence();
            Reset();
        }

        private void OnStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.Session != _playMan.LocalSession)
                return;

            // player finished fully connecting to the server.
            // OldStatus is used here because it can go from connecting-> connected or connecting-> ingame
            if (e.OldStatus == SessionStatus.Connecting)
                OnPlayerJoinedServer(e.Session);
            else if (e.NewStatus == SessionStatus.InGame)
                OnPlayerJoinedGame(e.Session);
        }

        private void OnRunLevelChanged(ClientRunLevel newRunLevel)
        {
            _logger.Debug($"Runlevel changed to: {newRunLevel}");
            var args = new RunLevelChangedEventArgs(RunLevel, newRunLevel);
            RunLevel = newRunLevel;
            RunLevelChanged?.Invoke(this, args);
        }

        void IPostInjectInit.PostInject()
        {
            _logger = _logMan.GetSawmill("client");
        }
    }

    /// <summary>
    ///     Enumeration of the run levels of the BaseClient.
    /// </summary>
    /// <seealso cref="ClientRunLevelExt"/>
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
    /// Helper functions for working with <see cref="ClientRunLevel"/>.
    /// </summary>
    public static class ClientRunLevelExt
    {
        /// <summary>
        /// Check if a <see cref="ClientRunLevel"/> is <see cref="ClientRunLevel.InGame"/>
        /// or <see cref="ClientRunLevel.SinglePlayerGame"/>.
        /// </summary>
        public static bool IsInGameLike(this ClientRunLevel runLevel)
        {
            return runLevel is ClientRunLevel.InGame or ClientRunLevel.SinglePlayerGame;
        }
    }

    /// <summary>
    ///     Event arguments for when something changed with the player.
    /// </summary>
    public sealed class PlayerEventArgs : EventArgs
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
    public sealed class RunLevelChangedEventArgs : EventArgs
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
    public sealed class ServerInfo
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

        public uint TickRate { get; internal set; }
    }
}
