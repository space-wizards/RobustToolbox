using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Lidgren.Network;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameState;
using SS14.Server.Interfaces.Log;
using SS14.Server.Interfaces.Map;
using SS14.Server.Interfaces.Placement;
using SS14.Server.Interfaces.Player;
using SS14.Server.Interfaces.Round;
using SS14.Server.Interfaces.ServerConsole;
using SS14.Server.Round;
using SS14.Shared;
using SS14.Shared.Configuration;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Prototypes;
using SS14.Shared.ServerEnums;
using SS14.Shared.Utility;

namespace SS14.Server
{
    /// <summary>
    /// Event delegate for when the run level of the BaseServer changes.
    /// </summary>
    /// <param name="oldLevel">Previous level.</param>
    /// <param name="newLevel">Net Level.</param>
    public delegate void EventRunLevelChanged(RunLevel oldLevel, RunLevel newLevel);

    /// <summary>
    /// Event delegate for when the server ticks.
    /// </summary>
    /// <param name="curTick">Current tick the server is at.</param>
    public delegate void EventTick(int curTick);

    /// <summary>
    /// The master class that runs the rest of the engine.
    /// </summary>
    public class BaseServer : IBaseServer
    {
        [Dependency]
        private readonly IConfigurationManager _config;

        [Dependency]
        private IComponentManager _components;

        [Dependency]
        private IServerEntityManager _entities;

        [Dependency]
        private IServerLogManager _logman;

        [Dependency]
        private readonly ISS14Serializer Serializer;

        public Shared.Network.Timing Time { get; private set; }

        private const int GAME_COUNTDOWN = 15;
        private static readonly AutoResetEvent AutoResetEvent = new AutoResetEvent(true);
        
        private RunLevel _runLevel;
        private bool _active;
        private int _lastAnnounced;
        private DateTime _startAt;

        private TimeSpan _lastTitleUpdate;
        private int _lastReceivedBytes;
        private int _lastSentBytes;
        private uint _lastState;

        private RunLevel Level
        {
            get => _runLevel;
            set
            {
                IoCManager.Resolve<IPlayerManager>().RunLevel = value;
                _runLevel = value;
            }
        }

        /// <inheritdoc />
        public string MapName => _config.GetCVar<string>("game.mapname");

        /// <inheritdoc />
        public int MaxPlayers => _config.GetCVar<int>("game.maxplayers");

        /// <inheritdoc />
        public string ServerName => _config.GetCVar<string>("game.hostname");

        /// <inheritdoc />
        public string Motd => _config.GetCVar<string>("game.welcomemsg");

        /// <inheritdoc />
        public event EventRunLevelChanged OnRunLevelChanged;

        /// <inheritdoc />
        public event EventTick OnTick;

        /// <inheritdoc />
        public void Restart()
        {
            //TODO: This needs to hard-reset all modules. The Game manager needs to control soft "round restarts".
            Logger.Info("[SRV] Soft restarting Server...");
            IoCManager.Resolve<IPlayerManager>().SendJoinLobbyToAll();
            SendGameStateUpdate(true);
            DisposeForRestart();
            StartLobby();
        }

        /// <inheritdoc />
        public void Shutdown(string reason = null)
        {
            if (string.IsNullOrWhiteSpace(reason))
                Logger.Log("[SRV] Shutting down...");
            else
                Logger.Log($"[SRV] {reason}, shutting down...");
            _active = false;
        }

        /// <inheritdoc />
        public void SaveGame()
        {
            IoCManager.Resolve<IMapManager>().SaveMap(MapName);
            _entities.SaveEntities();
        }

        /// <inheritdoc />
        public bool Start()
        {
            //Sets up the configMgr
            _config.LoadFromFile(PathHelpers.ExecutableRelativeFile("server_config.toml"));

            //Sets up Logging
            _config.RegisterCVar("log.path", "logs", CVarFlags.ARCHIVE);
            _config.RegisterCVar("log.format", "log_%(date)s-%(time)s.txt", CVarFlags.ARCHIVE);
            _config.RegisterCVar("log.level", LogLevel.Information, CVarFlags.ARCHIVE);

            var logPath = _config.GetCVar<string>("log.path");
            var logFormat = _config.GetCVar<string>("log.format");
            var logFilename = logFormat.Replace("%(date)s", DateTime.Now.ToString("yyyyMMdd")).Replace("%(time)s", DateTime.Now.ToString("hhmmss"));
            var fullPath = Path.Combine(logPath, logFilename);

            if (!Path.IsPathRooted(fullPath))
                logPath = PathHelpers.ExecutableRelativeFile(fullPath);

            // Create log directory if it does not exist yet.
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            _logman.CurrentLevel = _config.GetCVar<LogLevel>("log.level");
            _logman.LogPath = logPath;

            Time = new Shared.Network.Timing();

            Level = RunLevel.Init;

            LoadSettings();

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadDirectory(PathHelpers.ExecutableRelativeFile("Prototypes"));
            prototypeManager.Resync();

            var netMan = IoCManager.Resolve<IServerNetManager>();
            netMan.Initialize(true);

            //TODO: After the client gets migrated to new net system, hardcoded IDs will be removed, and these need to be put in their respective modules.
            netMan.RegisterNetMessage<MsgClGreet>(MsgClGreet.NAME, (int) MsgClGreet.ID, message => HandleClientGreet((MsgClGreet) message));
            netMan.RegisterNetMessage<MsgServerInfoReq>(MsgServerInfoReq.NAME, (int) MsgServerInfoReq.ID, HandleWelcomeMessageReq);
            netMan.RegisterNetMessage<MsgServerInfo>(MsgServerInfo.NAME, (int) MsgServerInfo.ID, HandleErrorMessage);
            netMan.RegisterNetMessage<MsgPlayerListReq>(MsgPlayerListReq.NAME, (int) MsgPlayerListReq.ID, HandlePlayerListReq);
            netMan.RegisterNetMessage<MsgPlayerList>(MsgPlayerList.NAME, (int) MsgPlayerList.ID, HandleErrorMessage);

            // Unused: NetMessages.LobbyChat
            netMan.RegisterNetMessage<MsgChat>(MsgChat.NAME, (int) MsgChat.ID, message => IoCManager.Resolve<IChatManager>().HandleNetMessage((MsgChat) message));
            netMan.RegisterNetMessage<MsgSession>(MsgSession.NAME, (int) MsgSession.ID, message => IoCManager.Resolve<IPlayerManager>().HandleNetworkMessage((MsgSession) message));
            netMan.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME, (int) MsgConCmd.ID, message => IoCManager.Resolve<IClientConsoleHost>().ProcessCommand((MsgConCmd) message));
            netMan.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME, (int) MsgConCmdAck.ID, HandleErrorMessage);
            netMan.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME, (int) MsgConCmdReg.ID, message => IoCManager.Resolve<IClientConsoleHost>().HandleRegistrationRequest(message.MsgChannel));

            netMan.RegisterNetMessage<MsgMapReq>(MsgMapReq.NAME, (int) MsgMapReq.ID, message => SendMap(message.MsgChannel));
            netMan.RegisterNetMessage<MsgMap>(MsgMap.NAME, (int) MsgMap.ID, message => IoCManager.Resolve<IMapManager>().HandleNetworkMessage((MsgMap) message));

            netMan.RegisterNetMessage<MsgPlacement>(MsgPlacement.NAME, (int) MsgPlacement.ID, message => IoCManager.Resolve<IPlacementManager>().HandleNetMessage((MsgPlacement) message));
            netMan.RegisterNetMessage<MsgUi>(MsgUi.NAME, (int) MsgUi.ID, HandleErrorMessage);
            netMan.RegisterNetMessage<MsgJoinGame>(MsgJoinGame.NAME, (int) MsgJoinGame.ID, HandleErrorMessage);
            netMan.RegisterNetMessage<MsgRestartReq>(MsgRestartReq.NAME, (int) MsgRestartReq.ID, message => Restart());

            netMan.RegisterNetMessage<MsgEntity>(MsgEntity.NAME, (int) MsgEntity.ID, message => _entities.HandleEntityNetworkMessage(((MsgEntity) message).Output));
            netMan.RegisterNetMessage<MsgAdmin>(MsgAdmin.NAME, (int) MsgAdmin.ID, message => HandleAdminMessage((MsgAdmin) message));
            // Not converted yet: NetMessages.StateUpdate
            netMan.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME, (int) MsgStateAck.ID, message => HandleStateAck((MsgStateAck) message));
            // Not converted yet: NetMessages.FullState

            Serializer.Initialize();
            IoCManager.Resolve<IChatManager>().Initialize();
            IoCManager.Resolve<IPlayerManager>().Initialize(this);
            IoCManager.Resolve<IMapManager>().Initialize();

            StartLobby();
            StartGame();

            _active = true;
            return false;
        }

        private TimeSpan _lastTick;
        private TimeSpan _lastKeepUpAnnounce;
        private TimeSpan _maxTime;

        /// <inheritdoc />
        public void MainLoop()
        {
            var tickRate = _config.GetCVar<int>("net.tickrate");

            // maximum number of ticks to queue before the loop slows down.
            const int MaxTicks = 5;

            Time.RegisterMainLoop(tickRate, () => AutoResetEvent.Set());

            Time.ResetRealTime();
            _maxTime = TimeSpan.FromTicks(Time.TickPeriod.Ticks * MaxTicks);

            while (_active)
            {
                // block the thread until the LoopTimer triggers
                AutoResetEvent.WaitOne(-1);

                var accumulator = Time.RealTime - _lastTick;

                // If the game can't keep up, limit time.
                if (accumulator > _maxTime)
                {
                    // limit accumulator to max time.
                    accumulator = _maxTime;

                    // pull lastTick up to the current realTime
                    // This will slow down the simulation, but if we are behind from a
                    // lag spike hopefully it will be able to catch up.
                    _lastTick = Time.RealTime - _maxTime;

                    // announce we are falling behind
                    if ((Time.RealTime - _lastKeepUpAnnounce).TotalSeconds >= 15.0)
                    {
                        Logger.Log("[SRV] MainLoop: Cannot keep up!");
                        _lastKeepUpAnnounce = Time.RealTime;
                    }
                }

                // run the simulation for every accumulated tick
                while (accumulator >= Time.TickPeriod)
                {
                    accumulator -= Time.TickPeriod;
                    _lastTick += Time.TickPeriod;
                    Time.StartFrame();
                    
                    Update((float)Time.CurTime.TotalSeconds);
                    Time.CurTick++;
                }

                // every 1 second update stats in the console window title
                if ((Time.RealTime - _lastTitleUpdate).TotalSeconds > 1.0)
                {
                    var netStats = UpdateBps();
                    Console.Title = $"FPS: {Math.Round(Time.FramesPerSecondAvg, 2):N2} SD:{Time.RealFrameTimeStdDev.TotalMilliseconds:N2}ms | Net: ({netStats}) | Memory: {Process.GetCurrentProcess().PrivateMemorySize64 >> 10:N0} KiB";
                    _lastTitleUpdate = Time.RealTime;
                }
            }

            Time.StopMainLoop();

            Cleanup();
        }

        /// <summary>
        ///     Loads the server settings from the ConfigurationManager.
        /// </summary>
        private void LoadSettings()
        {
            var cfgMgr = IoCManager.Resolve<IConfigurationManager>();

            cfgMgr.RegisterCVar("net.tickrate", 66, CVarFlags.ARCHIVE | CVarFlags.REPLICATED | CVarFlags.SERVER);

            cfgMgr.RegisterCVar("game.hostname", "MyServer", CVarFlags.ARCHIVE);
            cfgMgr.RegisterCVar("game.mapname", "SavedMap", CVarFlags.ARCHIVE);
            cfgMgr.RegisterCVar("game.maxplayers", 32, CVarFlags.ARCHIVE);
            cfgMgr.RegisterCVar("game.type", GameType.Game);
            cfgMgr.RegisterCVar("game.welcomemsg", "Welcome to the server!", CVarFlags.ARCHIVE);

            _entities = IoCManager.Resolve<IServerEntityManager>();
            _components = IoCManager.Resolve<IComponentManager>();

            Logger.Info($"[SRV] Name: {ServerName}");
            Logger.Info($"[SRV] TickRate: {Time.TickRate}({Time.TickPeriod.TotalMilliseconds:0.00}ms)");
            Logger.Info($"[SRV] Map: {MapName}");
            Logger.Info($"[SRV] Max players: {MaxPlayers}");
            Logger.Info($"[SRV] Welcome message: {Motd}");
        }

        /// <summary>
        ///     Controls what modules are running.
        /// </summary>
        /// <param name="runLevel"></param>
        private void InitModules(RunLevel runLevel = RunLevel.Lobby)
        {
            if (runLevel == Level)
                return;

            Level = runLevel;
            if (Level == RunLevel.Lobby)
            {
                _startAt = DateTime.Now.AddSeconds(GAME_COUNTDOWN);
            }
            else if (Level == RunLevel.Game)
            {
                IoCManager.Resolve<IMapManager>().LoadMap(MapName);
                _entities.Initialize();
                IoCManager.Resolve<IRoundManager>().CurrentGameMode.StartGame();
            }
        }

        private void StartLobby()
        {
            IoCManager.Resolve<IRoundManager>().Initialize(new Gamemode(this));
            InitModules();
        }

        /// <summary>
        ///     Moves all players to the game.
        /// </summary>
        private void StartGame()
        {
            InitModules(RunLevel.Game);
            IoCManager.Resolve<IPlayerManager>().SendJoinGameToAll();
        }

        private void DisposeForRestart()
        {
            IoCManager.Resolve<IPlayerManager>().DetachAll();
            _entities.Shutdown();
            GC.Collect();
        }

        private static void Cleanup()
        {
            Console.Title = "";
        }

        private string UpdateBps()
        {
            var stats = IoCManager.Resolve<IServerNetManager>().Statistics;

            var bps = $"Send: {(stats.SentBytes - _lastSentBytes) >> 10:N0} KiB/s, Recv: {(stats.ReceivedBytes - _lastReceivedBytes) >> 10:N0} KiB/s";

            _lastSentBytes = stats.SentBytes;
            _lastReceivedBytes = stats.ReceivedBytes;

            return bps;
        }

        private void Update(float frameTime)
        {
            IoCManager.Resolve<IServerNetManager>().ProcessPackets();

            switch (Level)
            {
                case RunLevel.Game:

                    _components.Update(frameTime);
                    _entities.Update(frameTime);
                    
                    IoCManager.Resolve<IRoundManager>().CurrentGameMode.Update();

                    break;

                case RunLevel.Lobby:

                    var countdown = _startAt.Subtract(DateTime.Now);
                    if (_lastAnnounced != countdown.Seconds)
                    {
                        _lastAnnounced = countdown.Seconds;
                        IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Server,
                            "Starting in " + _lastAnnounced + " seconds...",
                            "", 0);
                    }
                    if (countdown.Seconds <= 0)
                        StartGame();
                    break;
            }

            SendGameStateUpdate();
            IoCManager.Resolve<IConsoleManager>().Update();
        }

        private void SendGameStateUpdate(bool forceFullState = false)
        {
            var netMan = IoCManager.Resolve<IServerNetManager>();

            //Create a new GameState object
            var stateManager = IoCManager.Resolve<IGameStateManager>();
            var state = new GameState(Time.CurTick);
            if (_entities != null)
                state.EntityStates = _entities.GetEntityStates();
            state.PlayerStates = IoCManager.Resolve<IPlayerManager>().GetPlayerStates();
            stateManager.Add(state.Sequence, state);

            //_log.Log("Update " + _lastState + " sent.");
            var connections = netMan.Channels;
            if (!connections.Any())
            {
                //No clients -- don't send state
                stateManager.Clear();
            }
            else
            {
                foreach (var c in connections)
                    if (c.Connection.Status == NetConnectionStatus.Connected)
                    {
                        var session = IoCManager.Resolve<IPlayerManager>().GetSessionByChannel(c);
                        if (session == null || session.Status != SessionStatus.InGame && session.Status != SessionStatus.InLobby)
                            continue;
                        var stateMessage = netMan.Peer.CreateMessage();
                        var lastStateAcked = stateManager.GetLastStateAcked(c);

                        if (lastStateAcked == 0) // || forceFullState)
                        {
                            state.WriteStateMessage(stateMessage);
                            //_log.Log("Full state of size " + length + " sent to " + c.RemoteUniqueIdentifier);
                        }
                        else
                        {
                            stateMessage.Write((byte) NetMessages.StateUpdate);
                            var delta = stateManager.GetDelta(c, Time.CurTick);
                            delta.WriteDelta(stateMessage);
                            //_log.Log("Delta of size " + delta.Size + " sent to " + c.RemoteUniqueIdentifier);
                        }

                        netMan.Peer.SendMessage(stateMessage, c.Connection, NetDeliveryMethod.Unreliable);
                    }
            }
            stateManager.Cull();
        }

        #region MessageProcessing

        private void HandleWelcomeMessageReq(NetMessage message)
        {
            var net = IoCManager.Resolve<IServerNetManager>();
            var netMsg = message.MsgChannel.CreateNetMessage<MsgServerInfo>();

            netMsg.ServerName = ServerName;
            netMsg.ServerPort = net.Port;
            netMsg.ServerWelcomeMessage = Motd;
            netMsg.ServerMaxPlayers = MaxPlayers;
            netMsg.ServerMapName = MapName;
            netMsg.GameMode = IoCManager.Resolve<IRoundManager>().CurrentGameMode.Name;
            netMsg.ServerPlayerCount = IoCManager.Resolve<IPlayerManager>().PlayerCount;

            message.MsgChannel.SendMessage(netMsg);
        }

        private void HandleAdminMessage(MsgAdmin msg)
        {
            if (msg.MsgId == NetMessages.RequestEntityDeletion)
            {
                //TODO: Admin Permissions, requires admin system.
                //    IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection).
                //        adminPermissions.isAdmin || true)

                var delEnt = _entities.GetEntity(msg.EntityId);
                if (delEnt != null) _entities.DeleteEntity(delEnt);
            }
        }

        private static void HandleErrorMessage(NetMessage msg)
        {
            Logger.Error($"[SRV] Unhandled NetMessage type: {msg.MsgId}");
        }

        private static void HandlePlayerListReq(NetMessage message)
        {
            var channel = message.MsgChannel;
            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            var players = plyMgr.GetAllPlayers().ToArray();
            var netMsg = channel.CreateNetMessage<MsgPlayerList>();
            netMsg.PlyCount = (byte) players.Length;

            var list = new List<MsgPlayerList.PlyInfo>();
            foreach (var client in players)
            {
                var info = new MsgPlayerList.PlyInfo
                {
                    Name = client.Name,
                    Status = (byte) client.Status,
                    Ping = client.ConnectedClient.Connection.AverageRoundtripTime
                };
                list.Add(info);
            }
            netMsg.Plyrs = list;

            channel.SendMessage(netMsg);
        }

        private static void HandleClientGreet(MsgClGreet msg)
        {
            var fixedName = msg.PlyName.Trim();
            if (fixedName.Length < 3)
                fixedName = $"Player {msg.MsgChannel.NetworkId}";

            var p = IoCManager.Resolve<IPlayerManager>().GetSessionByChannel(msg.MsgChannel);
            p.SetName(fixedName);
        }

        private static void HandleStateAck(MsgStateAck msg)
        {
            IoCManager.Resolve<IGameStateManager>().Ack(msg.MsgChannel.ConnectionId, msg.Sequence);
            //_log.Log("State Acked: " + sequence + " by client " + msg.SenderConnection.RemoteUniqueIdentifier + ".");
        }

        // The size of the map being sent is almost exactly 1 byte per tile.
        // The default 30x30 map is 900 bytes, a 100x100 one is 10,000 bytes (10kb).
        private static void SendMap(INetChannel client)
        {
            // Send Tiles
            IoCManager.Resolve<IMapManager>().SendMap(client);

            // Lets also send them all the items and mobs.
            //_entities.SendEntities(client);

            // Send atmos state to player
            //IoCManager.Resolve<IAtmosManager>().SendAtmosStateTo(client);

            // Todo: Preempt this with the lobby.
            IoCManager.Resolve<IRoundManager>().SpawnPlayer(
                IoCManager.Resolve<IPlayerManager>().GetSessionById(client.NetworkId)); //SPAWN PLAYER
        }

        #endregion
    }
}
