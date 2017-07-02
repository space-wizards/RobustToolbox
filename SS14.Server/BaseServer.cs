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
using SS14.Server.Timing;
using SS14.Shared;
using SS14.Shared.Configuration;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Prototypes;
using SS14.Shared.ServerEnums;
using SS14.Shared.Utility;

namespace SS14.Server
{
    public delegate void EventRunLevelChanged(RunLevel oldLevel, RunLevel newLevel);
    public delegate void EventTick(int curTick);

    public class BaseServer : IBaseServer
    {
        private const int GAME_COUNTDOWN = 15;
        private const int UPDATE_RATE = 20; //20 updates per second
        private static readonly AutoResetEvent AutoResetEvent = new AutoResetEvent(true);
        private readonly IComponentManager _components;
        private readonly IServerEntityManager _entities;
        private readonly List<float> _frameTimes = new List<float>();
        private readonly IServerLogManager _log;
        private readonly Stopwatch _stopWatch = new Stopwatch();
        private bool _active;
        private uint _basePeriod;
        private int _lastAnnounced;
        private uint _lastState;
        private DateTime _lastStateTime = DateTime.Now;
        private uint _oldestAckedState;
        private uint _period;
        private RunLevel _runLevel;
        private float _serverClock;
        private DateTime _startAt;
        private DateTime _time;

        /// <summary>
        ///     Constructs an instance of the server.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="logManager"></param>
        public BaseServer(ICommandLineArgs args, IServerLogManager logManager)
        {
            _log = logManager;
            Level = RunLevel.Init;

            var configMgr = IoCManager.Resolve<IConfigurationManager>();
            configMgr.LoadFromFile(PathHelpers.ExecutableRelativeFile("server_config.toml"));

            configMgr.RegisterCVar("log.path", "logs", CVarFlags.ARCHIVE);
            configMgr.RegisterCVar("log.format", "log_%(date)s-%(time)s.txt", CVarFlags.ARCHIVE);
            configMgr.RegisterCVar("log.level", LogLevel.Information, CVarFlags.ARCHIVE);
            configMgr.RegisterCVar("log.enabled", true, CVarFlags.ARCHIVE);

            configMgr.RegisterCVar("net.tickrate", 66, CVarFlags.ARCHIVE | CVarFlags.REPLICATED | CVarFlags.SERVER);

            var logPath = configMgr.GetCVar<string>("log.path");
            var logFormat = configMgr.GetCVar<string>("log.format");
            var logFilename = logFormat.Replace("%(date)s", DateTime.Now.ToString("yyyyMMdd")).Replace("%(time)s", DateTime.Now.ToString("hhmmss"));
            var fullPath = Path.Combine(logPath, logFilename);

            if (!Path.IsPathRooted(fullPath))
                logPath = PathHelpers.ExecutableRelativeFile(fullPath);

            // Create log directory if it does not exist yet.
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));

            _log.CurrentLevel = configMgr.GetCVar<LogLevel>("log.level");
            _log.LogPath = logPath;

            _tickRate = configMgr.GetCVar<int>("net.tickrate");
            _serverRate = 1000.0f / _tickRate;

            _entities = IoCManager.Resolve<IServerEntityManager>();
            _components = IoCManager.Resolve<IComponentManager>();
        }

        [Obsolete] //TODO: Kill this
        private NetworkServer NetServer => IoCManager.Resolve<INetworkServer>() as NetworkServer;

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
        public event EventRunLevelChanged OnRunLevelChanged;

        /// <inheritdoc />
        public event EventTick OnTick;

        /// <summary>
        ///     Loads the server settings from the ConfigurationManager.
        /// </summary>
        private void LoadSettings()
        {
            var cfgMgr = IoCManager.Resolve<IConfigurationManager>();

            cfgMgr.RegisterCVar("game.hostname", "MyServer", CVarFlags.ARCHIVE);
            cfgMgr.RegisterCVar("game.mapname", "SavedMap", CVarFlags.ARCHIVE);
            cfgMgr.RegisterCVar("game.maxplayers", 32, CVarFlags.ARCHIVE);
            cfgMgr.RegisterCVar("game.type", GameType.Game);
            cfgMgr.RegisterCVar("game.welcomemsg", "Welcome to the server!", CVarFlags.ARCHIVE);

            _serverPort = cfgMgr.GetCVar<int>("net.port");
            _serverName = cfgMgr.GetCVar<string>("game.hostname");
            _serverMapName = cfgMgr.GetCVar<string>("game.mapname");
            _serverMaxPlayers = cfgMgr.GetCVar<int>("game.maxplayers");
            _gameType = cfgMgr.GetCVar<GameType>("game.type");
            _serverWelcomeMessage = cfgMgr.GetCVar<string>("game.welcomemsg");

            Logger.Info($"[SRV] Port: {_serverPort}");
            Logger.Info($"[SRV] Name: {_serverName}");
            Logger.Info($"[SRV] TickRate: {_tickRate}({_serverRate}ms)");
            Logger.Info($"[SRV] Map: {_serverMapName}");
            Logger.Info($"[SRV] Max players: {_serverMaxPlayers}");
            Logger.Info($"[SRV] Game type: {_gameType}");
            Logger.Info($"[SRV] Welcome message: {_serverWelcomeMessage}");
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
                IoCManager.Resolve<IMapManager>().LoadMap(_serverMapName);
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

        #region Server Settings

        private DateTime _lastUpdate;
        private GameType _gameType = GameType.Game;
        private string _serverMapName = "SavedMap";
        private int _serverMaxPlayers = 32;
        private string _serverName = "SS13 Server";
        private int _serverPort = 1212;
        private string _serverWelcomeMessage = "Welcome to the server!";
        private DateTime _lastBytesUpdate = DateTime.Now;
        private int _lastReceivedBytes;
        private int _lastSentBytes;

        private readonly float _serverRate; // desired server frame (tick) time in milliseconds
        private readonly float _tickRate; // desired server frames (ticks) per second

        #endregion Server Settings

        #region IBaseServer Members

        /// <inheritdoc />
        public void Restart()
        {
            //TODO: This needs to hard-reset all modules.
            Logger.Info("[SRV] Soft restarting Server...");
            IoCManager.Resolve<IPlayerManager>().SendJoinLobbyToAll();
            SendGameStateUpdate(true, true);
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
            IoCManager.Resolve<IMapManager>().SaveMap(_serverMapName);
            _entities.SaveEntities();
        }

        /// <inheritdoc />
        public bool Start()
        {
            _time = DateTime.Now;

            LoadSettings();

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadDirectory(PathHelpers.ExecutableRelativeFile("Prototypes"));
            prototypeManager.Resync();

            var netMan = IoCManager.Resolve<INetworkServer>();
            netMan.Initialize(true);

            //TODO: Register all NetMessages
            netMan.RegisterNetMessage<MsgPlayerListReq>(MsgPlayerListReq.NAME, (int)MsgPlayerListReq.ID, HandleGenericMessage);

            IoCManager.Resolve<IChatManager>().Initialize(this);
            IoCManager.Resolve<IPlayerManager>().Initialize(this);
            IoCManager.Resolve<IPlacementManager>().Initialize(this);

            StartLobby();
            StartGame();

            _active = true;
            return false;
        }

        #endregion

        #region server mainloop

        /// <inheritdoc />
        public void MainLoop()
        {
            _basePeriod = 1;
            _period = _basePeriod;

            var timerObject = new MainLoopTimer();
            _stopWatch.Start();
            timerObject.mainLoopTimer.CreateMainLoopTimer(RunLoop, _period);

            while (_active)
            {
                AutoResetEvent.WaitOne(-1);

                DoMainLoopStuff();
            }

            Cleanup();

            /*   TimerCallback tcb = RunLoop;
            var due = 1;// (long)ServerRate / 3;
            stopWatch.Start(); //Start the clock
            mainLoopTimer = new Timer(tcb, _autoResetEvent, 0, due);
            _autoResetEvent.WaitOne(-1);*/
        }

        private static void RunLoop()
        {
            AutoResetEvent.Set();
        }

        private void DoMainLoopStuff()
        {
            var elapsedTime = _stopWatch.ElapsedTicks / (float) Stopwatch.Frequency;
            var elapsedMilliseconds = elapsedTime * 1000;

            if (elapsedMilliseconds < _serverRate && _serverRate - elapsedMilliseconds >= 0.5f)
                return;
            _stopWatch.Restart(); //Reset the stopwatch so we get elapsed time next time

            //Elapsed time in seconds since the last tick
            _serverClock += elapsedTime;

            //Begin update time
            _time = DateTime.Now;
            if (_frameTimes.Count >= _tickRate)
                _frameTimes.RemoveAt(0);
            var rate = 1 / elapsedTime;
            _frameTimes.Add(rate);

            if ((DateTime.Now - _lastBytesUpdate).TotalMilliseconds > 1000)
            {
                var netStats = UpdateBps();
                Console.Title = string.Format("FPS: {0:N2} | Net: ({1}) | Memory: {2:N0} KiB",
                    Math.Round(FrameTimeAverage(), 2),
                    netStats,
                    Process.GetCurrentProcess().PrivateMemorySize64 >> 10);
                _lastBytesUpdate = DateTime.Now;
            }

            IoCManager.Resolve<INetworkServer>().ProcessPackets();

            //Update takes elapsed time in seconds.
            Update(elapsedTime);

            IoCManager.Resolve<IConsoleManager>().Update();
        }

        private static void Cleanup()
        {
            Console.Title = "";
        }

        private string UpdateBps()
        {
            var bps = string.Format("Send: {0:N0} KiB/s, Recv: {1:N0} KiB/s",
                (IoCManager.Resolve<INetworkServer>().Statistics.SentBytes - _lastSentBytes) >> 10,
                (IoCManager.Resolve<INetworkServer>().Statistics.ReceivedBytes - _lastReceivedBytes) >> 10);
            _lastSentBytes = IoCManager.Resolve<INetworkServer>().Statistics.SentBytes;
            _lastReceivedBytes = IoCManager.Resolve<INetworkServer>().Statistics.ReceivedBytes;
            return bps;
        }

        private float FrameTimeAverage()
        {
            if (_frameTimes.Count == 0)
                return 0;
            return _frameTimes.Average(p => p);
        }

        private void Update(float frameTime)
        {
            switch (Level)
            {
                case RunLevel.Game:

                    _components.Update(frameTime);
                    _entities.Update(frameTime);
                    var start = _stopWatch.ElapsedTicks;
                    //((AtmosManager)IoCManager.Resolve<IAtmosManager>()).Update(frameTime);
                    var end = _stopWatch.ElapsedTicks;
                    var atmosTime = (end - start) / (float) Stopwatch.Frequency * 1000;
                    IoCManager.Resolve<IRoundManager>().CurrentGameMode.Update();
                    GC.KeepAlive(atmosTime);

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
            _lastUpdate = _time;
            SendGameStateUpdate();
        }

        private void SendGameStateUpdate(bool force = false, bool forceFullState = false)
        {
            //Obey the updates per second limit
            var elapsed = _time - _lastStateTime;
            if (force || elapsed.TotalMilliseconds > 1000 / UPDATE_RATE)
            {
                //Save last state time
                _lastStateTime = _time;
                //Create a new GameState object
                var stateManager = IoCManager.Resolve<IGameStateManager>();
                var state = new GameState(++_lastState);
                if (_entities != null)
                    state.EntityStates = _entities.GetEntityStates();
                state.PlayerStates = IoCManager.Resolve<IPlayerManager>().GetPlayerStates();
                stateManager.Add(state.Sequence, state);

                //_log.Log("Update " + _lastState + " sent.");
                var connections = IoCManager.Resolve<INetworkServer>().Connections;
                if (!connections.Any())
                {
                    //No clients -- don't send state
                    _oldestAckedState = _lastState;
                    stateManager.Clear();
                }
                else
                {
                    foreach (var c in connections)
                        if (c.Connection.Status == NetConnectionStatus.Connected)
                        {
                            var session = IoCManager.Resolve<IPlayerManager>().GetSessionById(c.NetworkId);
                            if (session == null || session.Status != SessionStatus.InGame && session.Status != SessionStatus.InLobby)
                                continue;
                            var stateMessage = IoCManager.Resolve<INetworkServer>().Server.CreateMessage();
                            var lastStateAcked = stateManager.GetLastStateAcked(c.Connection);

                            if (lastStateAcked == 0) // || forceFullState)
                            {
                                state.WriteStateMessage(stateMessage);
                                //_log.Log("Full state of size " + length + " sent to " + c.RemoteUniqueIdentifier);
                            }
                            else
                            {
                                stateMessage.Write((byte) NetMessages.StateUpdate);
                                var delta = stateManager.GetDelta(c.Connection, _lastState);
                                delta.WriteDelta(stateMessage);
                                //_log.Log("Delta of size " + delta.Size + " sent to " + c.RemoteUniqueIdentifier);
                            }

                            IoCManager.Resolve<INetworkServer>().Server.SendMessage(stateMessage, c.Connection, NetDeliveryMethod.Unreliable);
                        }
                }
                stateManager.Cull();
            }
        }

        #endregion server mainloop

        #region MessageProcessing

        /// <summary>
        ///     Main method for routing incoming application network messages
        /// </summary>
        /// <param name="msg"></param>
        private void HandleGenericMessage(NetMessage msg)
        {
            var chan = msg.Channel;
            Logger.Info($"[NET] Received message {chan}:{msg.Name}");
            var messageType = msg.Id;
            var channel = chan;

            switch (messageType)
            {
                case NetMessages.WelcomeMessage:
                    {
                        var netMsg = channel.CreateMessage<MsgServerInfo>();

                        netMsg.ServerName = _serverName;
                        netMsg.ServerPort = _serverPort;
                        netMsg.ServerWelcomeMessage = _serverWelcomeMessage;
                        netMsg.ServerMaxPlayers = _serverMaxPlayers;
                        netMsg.ServerMapName = _serverMapName;
                        netMsg.GameMode = IoCManager.Resolve<IRoundManager>().CurrentGameMode.Name;
                        netMsg.ServerPlayerCount = NetServer.ConnectionCount;

                        channel.SendMessage(netMsg);
                    }
                    break;

                case NetMessages.ForceRestart:
                    Restart();
                    break;

                case NetMessages.RequestMap:
                    SendMap(chan);
                    break;

                case NetMessages.PlayerListReq:
                    {
                        var netMsg = channel.CreateMessage<MsgPlayerList>();
                        netMsg.PlyCount = (byte) NetServer.ConnectionCount;

                        var list = new List<MsgPlayerList.PlyInfo>();
                        foreach (var client in NetServer.Connections)
                        {
                            var info = new MsgPlayerList.PlyInfo();
                            var plrSession = IoCManager.Resolve<IPlayerManager>().GetSessionById(client.NetworkId);

                            info.name = plrSession.Name;
                            info.status = (byte) plrSession.Status;
                            info.ping = client.Connection.AverageRoundtripTime;
                            list.Add(info);
                        }
                        netMsg.plyrs = list;

                        channel.SendMessage(netMsg);
                    }
                    break;

                case NetMessages.ClientName:
                    HandleClientGreet((MsgClGreet)msg);
                    break;

                case NetMessages.ChatMessage:
                    IoCManager.Resolve<IChatManager>().HandleNetMessage((MsgChat)msg);
                    break;

                case NetMessages.PlayerSessionMessage:
                    IoCManager.Resolve<IPlayerManager>().HandleNetworkMessage((MsgSession)msg);
                    break;

                case NetMessages.MapMessage:
                    IoCManager.Resolve<IMapManager>().HandleNetworkMessage((MsgMap)msg);
                    break;

                case NetMessages.PlacementManagerMessage:
                    IoCManager.Resolve<IPlacementManager>().HandleNetMessage((MsgPlacement)msg);
                    break;

                case NetMessages.EntityMessage:
                    _entities.HandleEntityNetworkMessage(((MsgEntity)msg).Output);
                    break;

                case NetMessages.RequestEntityDeletion:
                    HandleAdminMessage(messageType, (MsgAdmin)msg);
                    break;

                case NetMessages.StateAck:
                    HandleStateAck((MsgStateAck)msg);
                    break;

                case NetMessages.ConsoleCommand:
                {
                    var msgCmd = (MsgConCmd) msg;
                        IoCManager.Resolve<IClientConsoleHost>().ProcessCommand(msgCmd.text, msg.Channel.Connection);
                    }
                    break;

                case NetMessages.ConsoleCommandRegister:
                    IoCManager.Resolve<IClientConsoleHost>().HandleRegistrationRequest(msg.Channel.Connection);
                    break;

                default:
                    Logger.Error($"[SRV] Unhandled NetMessage type: {msg.Id}");
                    break;
            }
        }

        private void HandleAdminMessage(NetMessages adminMsgType, MsgAdmin msg)
        {
            switch (adminMsgType)
            {
                case NetMessages.RequestEntityDeletion:

                    //TODO: Admin Permissions, requires admin system.
                    //    IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection).
                    //        adminPermissions.isAdmin || true)

                    var delEnt = _entities.GetEntity(msg.EntityId);
                    if (delEnt != null) _entities.DeleteEntity(delEnt);
                    break;
            }
        }

        private static void HandleClientGreet(MsgClGreet msg)
        {
            var fixedName = msg.Name.Trim();
            if (fixedName.Length < 3)
                fixedName = $"Player {msg.Channel.NetworkId}";

            msg.Channel.PlayerName = fixedName;
            var p = IoCManager.Resolve<IPlayerManager>().GetSessionByChannel(msg.Channel);
            p.SetName(fixedName);
        }

        private static void HandleStateAck(MsgStateAck msg)
        {
            IoCManager.Resolve<IGameStateManager>().Ack(msg.Channel.UUID, msg.Sequence);
            //_log.Log("State Acked: " + sequence + " by client " + msg.SenderConnection.RemoteUniqueIdentifier + ".");
        }

        // The size of the map being sent is almost exaclty 1 byte per tile.
        // The default 30x30 map is 900 bytes, a 100x100 one is 10,000 bytes (10kb).
        private static void SendMap(NetChannel client)
        {
            // Send Tiles
            IoCManager.Resolve<IMapManager>().SendMap(client.Connection);

            // Lets also send them all the items and mobs.
            //_entities.SendEntities(client);

            // Send atmos state to player
            //IoCManager.Resolve<IAtmosManager>().SendAtmosStateTo(client);

            // Todo: Preempt this with the lobby.
            IoCManager.Resolve<IRoundManager>().SpawnPlayer(
                IoCManager.Resolve<IPlayerManager>().GetSessionById(client.NetworkId)); //SPAWN PLAYER
        }

        public void SendChangeTile(int x, int y, Tile newTile)
        {
            var tileMessage = IoCManager.Resolve<INetworkServer>().Server.CreateMessage();
            //tileMessage.Write((byte)NetMessages.ChangeTile);
            tileMessage.Write(x);
            tileMessage.Write(y);
            tileMessage.Write((uint) newTile);
            foreach (var connection in NetServer.Connections)
            {
                IoCManager.Resolve<INetworkServer>().Server.SendMessage(tileMessage, connection.Connection,
                    NetDeliveryMethod.ReliableOrdered);
                Logger.Log(connection.Connection.RemoteEndPoint.Address + ": Tile Change Being Sent", LogLevel.Debug);
            }
        }

        #endregion
    }
}
