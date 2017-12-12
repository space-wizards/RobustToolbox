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
using SS14.Server.Interfaces.Placement;
using SS14.Server.Interfaces.Player;
using SS14.Server.Interfaces.Round;
using SS14.Server.Interfaces.ServerConsole;
using SS14.Server.Round;
using SS14.Shared;
using SS14.Shared.Configuration;
using SS14.Shared.ContentPack;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Prototypes;
using SS14.Shared.ServerEnums;
using SS14.Shared.Map;
using SS14.Server.Interfaces.Maps;

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
        private readonly ICommandLineArgs _commandLine;
        [Dependency]
        private readonly IConfigurationManager _config;
        [Dependency]
        private readonly IComponentManager _components;
        [Dependency]
        private readonly IServerEntityManager _entities;
        [Dependency]
        private readonly IServerLogManager _logman;
        [Dependency]
        private readonly ISS14Serializer Serializer;
        [Dependency]
        private readonly IGameTiming _time;
        [Dependency]
        private readonly IResourceManager _resources;
        [Dependency]
        private readonly IMapLoader mapLoader;
        [Dependency]
        private readonly IMapManager mapManager;

        private const int GAME_COUNTDOWN = 15;

        private RunLevel _runLevel;
        private bool _active;
        private int _lastAnnounced;
        private DateTime _startAt;

        private TimeSpan _lastTitleUpdate;
        private int _lastReceivedBytes;
        private int _lastSentBytes;

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
        public void Restart()
        {
            //TODO: This needs to hard-reset all modules. The Game manager needs to control soft "round restarts".
            Logger.Info("[SRV] Soft restarting Server...");
            IoCManager.Resolve<IPlayerManager>().SendJoinLobbyToAll();
            SendGameStateUpdate();
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
            mapLoader.Save(PathHelpers.ExecutableRelativeFile(Path.Combine("Resources", MapName)), mapManager.GetMap(1));
        }

        /// <inheritdoc />
        public bool Start()
        {
            //Sets up the configMgr
            _config.LoadFromFile(_commandLine.ConfigFile);

            //Sets up Logging
            _config.RegisterCVar("log.path", "logs", CVar.ARCHIVE);
            _config.RegisterCVar("log.format", "log_%(date)s-%(time)s.txt", CVar.ARCHIVE);
            _config.RegisterCVar("log.level", LogLevel.Information, CVar.ARCHIVE);

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

            Level = RunLevel.Init;

            LoadSettings();

            var netMan = IoCManager.Resolve<IServerNetManager>();
            try
            {
                netMan.Initialize(true);
                netMan.Startup();
            }
            catch (System.Net.Sockets.SocketException e)
            {
                var port = netMan.Port;
                Logger.Log($"Unable to setup networking manager. Check port {port} is not already in use!, shutting down...", LogLevel.Fatal);
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to setup networking manager. Unknown exception: {e}, shutting down...", LogLevel.Fatal);
                Environment.Exit(1);
            }

            //TODO: After the client gets migrated to new net system, hardcoded IDs will be removed, and these need to be put in their respective modules.
            netMan.RegisterNetMessage<MsgClGreet>(MsgClGreet.NAME, (int)MsgClGreet.ID, message => HandleClientGreet((MsgClGreet)message));
            netMan.RegisterNetMessage<MsgServerInfoReq>(MsgServerInfoReq.NAME, (int)MsgServerInfoReq.ID, HandleWelcomeMessageReq);
            netMan.RegisterNetMessage<MsgServerInfo>(MsgServerInfo.NAME, (int)MsgServerInfo.ID, HandleErrorMessage);
            netMan.RegisterNetMessage<MsgPlayerListReq>(MsgPlayerListReq.NAME, (int)MsgPlayerListReq.ID, HandlePlayerListReq);
            netMan.RegisterNetMessage<MsgPlayerList>(MsgPlayerList.NAME, (int)MsgPlayerList.ID, HandleErrorMessage);

            // Unused: NetMessages.LobbyChat
            netMan.RegisterNetMessage<MsgChat>(MsgChat.NAME, (int)MsgChat.ID, message => IoCManager.Resolve<IChatManager>().HandleNetMessage((MsgChat)message));
            netMan.RegisterNetMessage<MsgSession>(MsgSession.NAME, (int)MsgSession.ID, message => IoCManager.Resolve<IPlayerManager>().HandleNetworkMessage((MsgSession)message));
            netMan.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME, (int)MsgConCmd.ID, message => IoCManager.Resolve<IClientConsoleHost>().ProcessCommand((MsgConCmd)message));
            netMan.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME, (int)MsgConCmdAck.ID, HandleErrorMessage);
            netMan.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME, (int)MsgConCmdReg.ID, message => IoCManager.Resolve<IClientConsoleHost>().HandleRegistrationRequest(message.MsgChannel));

            netMan.RegisterNetMessage<MsgMapReq>(MsgMapReq.NAME, (int)MsgMapReq.ID, message => SendMap(message.MsgChannel));

            netMan.RegisterNetMessage<MsgPlacement>(MsgPlacement.NAME, (int)MsgPlacement.ID, message => IoCManager.Resolve<IPlacementManager>().HandleNetMessage((MsgPlacement)message));
            netMan.RegisterNetMessage<MsgUi>(MsgUi.NAME, (int)MsgUi.ID, HandleErrorMessage);
            netMan.RegisterNetMessage<MsgJoinGame>(MsgJoinGame.NAME, (int)MsgJoinGame.ID, HandleErrorMessage);
            netMan.RegisterNetMessage<MsgRestartReq>(MsgRestartReq.NAME, (int)MsgRestartReq.ID, message => Restart());

            netMan.RegisterNetMessage<MsgEntity>(MsgEntity.NAME, (int)MsgEntity.ID, message => _entities.HandleEntityNetworkMessage((MsgEntity)message));
            netMan.RegisterNetMessage<MsgAdmin>(MsgAdmin.NAME, (int)MsgAdmin.ID, message => HandleAdminMessage((MsgAdmin)message));
            netMan.RegisterNetMessage<MsgStateUpdate>(MsgStateUpdate.NAME, (int)MsgStateUpdate.ID, message => HandleErrorMessage(message));
            netMan.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME, (int)MsgStateAck.ID, message => HandleStateAck((MsgStateAck)message));
            netMan.RegisterNetMessage<MsgFullState>(MsgFullState.NAME, (int)MsgFullState.ID, message => HandleErrorMessage(message));

            IoCManager.Resolve<IChatManager>().Initialize();
            IoCManager.Resolve<IPlayerManager>().Initialize(this, MaxPlayers);
            IoCManager.Resolve<IMapManager>().Initialize();

            // Set up the VFS
            _resources.Initialize();

            _resources.MountContentDirectory(@"./Resources/");

            //mount the engine content pack
            _resources.MountContentPack(@"EngineContentPack.zip");

            //mount the default game ContentPack defined in config
            _resources.MountDefaultContentPack();

            LoadContentAssembly<GameShared>("Shared");
            LoadContentAssembly<GameServer>("Server");

            // HAS to happen after content gets loaded.
            // Else the content types won't be included.
            // TODO: solve this properly.
            Serializer.Initialize();

            // Call Init in game assemblies.
            AssemblyLoader.BroadcastRunLevel(AssemblyLoader.RunLevel.Init);

            // because of 'reasons' this has to be called after the last assembly is loaded
            // otherwise the prototypes will be cleared
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadDirectory(@"Prototypes");
            prototypeManager.Resync();

            var clientConsole = IoCManager.Resolve<IClientConsoleHost>();
            clientConsole.Initialize();
            var consoleManager = IoCManager.Resolve<IConsoleManager>();
            consoleManager.Initialize();

            StartLobby();
            StartGame();

            _active = true;
            return false;
        }

        private void LoadContentAssembly<T>(string name) where T : GameShared
        {
            // get the assembly from the file system
            if (_resources.TryContentFileRead($@"Assemblies/Content.{name}.dll", out MemoryStream gameDll))
            {
                Logger.Debug($"[SRV] Loading {name} Content DLL");

                // see if debug info is present
                if (_resources.TryContentFileRead($@"Assemblies/Content.{name}.pdb", out MemoryStream gamePdb))
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        AssemblyLoader.LoadGameAssembly<T>(gameDll.ToArray(), gamePdb.ToArray());
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"[SRV] Exception loading DLL Content.{name}.dll: {e}");
                    }
                }
                else
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        AssemblyLoader.LoadGameAssembly<T>(gameDll.ToArray());
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"[SRV] Exception loading DLL Content.{name}.dll: {e}");
                    }
                }
            }
            else
            {
                Logger.Warning($"[ENG] Could not find {name} Content DLL");
            }
        }

        private TimeSpan _lastTick;
        private TimeSpan _lastKeepUpAnnounce;

        /// <inheritdoc />
        public void MainLoop()
        {
            // maximum number of ticks to queue before the loop slows down.
            const int maxTicks = 5;

            _time.ResetRealTime();
            var maxTime = TimeSpan.FromTicks(_time.TickPeriod.Ticks * maxTicks);

            while (_active)
            {
                var accumulator = _time.RealTime - _lastTick;

                // If the game can't keep up, limit time.
                if (accumulator > maxTime)
                {
                    // limit accumulator to max time.
                    accumulator = maxTime;

                    // pull lastTick up to the current realTime
                    // This will slow down the simulation, but if we are behind from a
                    // lag spike hopefully it will be able to catch up.
                    _lastTick = _time.RealTime - maxTime;

                    // announce we are falling behind
                    if ((_time.RealTime - _lastKeepUpAnnounce).TotalSeconds >= 15.0)
                    {
                        Logger.Warning("[SRV] MainLoop: Cannot keep up!");
                        _lastKeepUpAnnounce = _time.RealTime;
                    }
                }

                _time.InSimulation = true;

                // run the simulation for every accumulated tick
                while (accumulator >= _time.TickPeriod)
                {
                    accumulator -= _time.TickPeriod;
                    _lastTick += _time.TickPeriod;
                    _time.StartFrame();

                    // only run the sim if unpaused, but still use up the accumulated time
                    if (!_time.Paused)
                    {
                        Update((float)_time.FrameTime.TotalSeconds);
                        _time.CurTick++;
                    }
                }

                // if not paused, save how far between ticks we are so interpolation works
                if (!_time.Paused)
                    _time.TickRemainder = accumulator;

                _time.InSimulation = false;

                // every 1 second update stats in the console window title
                if ((_time.RealTime - _lastTitleUpdate).TotalSeconds > 1.0)
                {
                    var netStats = UpdateBps();
                    Console.Title = string.Format("FPS: {0:N2} SD:{1:N2}ms | Net: ({2}) | Memory: {3:N0} KiB",
                        Math.Round(_time.FramesPerSecondAvg, 2),
                        _time.RealFrameTimeStdDev.TotalMilliseconds,
                        netStats,
                        Process.GetCurrentProcess().PrivateMemorySize64 >> 10);
                    _lastTitleUpdate = _time.RealTime;
                }

                // Set this to 1 if you want to be nice and give the rest of the timeslice up to the os scheduler.
                // Set this to 0 if you want to use 100% cpu, but still cooperate with the scheduler.
                // comment this out if you want to be 'that thread' and hog 100% cpu.
                Thread.Sleep(1);
            }

            Cleanup();
        }

        /// <summary>
        ///     Loads the server settings from the ConfigurationManager.
        /// </summary>
        private void LoadSettings()
        {
            var cfgMgr = IoCManager.Resolve<IConfigurationManager>();

            cfgMgr.RegisterCVar("net.tickrate", 66, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

            cfgMgr.RegisterCVar("game.hostname", "MyServer", CVar.ARCHIVE);
            cfgMgr.RegisterCVar("game.mapname", "SavedEntities.xml", CVar.ARCHIVE);
            cfgMgr.RegisterCVar("game.maxplayers", 32, CVar.ARCHIVE);
            cfgMgr.RegisterCVar("game.type", GameType.Game);
            cfgMgr.RegisterCVar("game.welcomemsg", "Welcome to the server!", CVar.ARCHIVE);

            _time.TickRate = _config.GetCVar<int>("net.tickrate");

            Logger.Info($"[SRV] Name: {ServerName}");
            Logger.Info($"[SRV] TickRate: {_time.TickRate}({_time.TickPeriod.TotalMilliseconds:0.00}ms)");
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

            var oldLevel = Level;
            Level = runLevel;
            OnRunLevelChanged?.Invoke(oldLevel, Level);
            if (Level == RunLevel.Lobby)
            {
                _startAt = DateTime.Now.AddSeconds(GAME_COUNTDOWN);
            }
            else if (Level == RunLevel.Game)
            {
                LoadMap(MapName);
                _entities.Initialize();
                IoCManager.Resolve<IRoundManager>().CurrentGameMode.StartGame();
            }
        }

        #region File Operations

        public bool LoadMap(string mapName)
        {
            var defManager = IoCManager.Resolve<ITileDefinitionManager>();
            var mapMgr = IoCManager.Resolve<IMapManager>();

            NewDefaultMap(mapMgr, defManager, 1);
            mapLoader.Load(MapName, mapMgr.GetMap(1));

            return true;
        }

        //TODO: This whole method should be removed once file loading/saving works, and replaced with a 'Demo' map.
        /// <summary>
        ///     Generates 'Demo' grid and inserts it into the map manager.
        /// </summary>
        /// <param name="mapManager">The map manager to work with.</param>
        /// <param name="defManager">The definition manager to work with.</param>
        /// <param name="gridId">The ID of the grid to generate and insert into the map manager.</param>
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

        #endregion File Operations

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
            if(Level == RunLevel.Game)
            {
                var mapMgr = IoCManager.Resolve<IMapManager>();

                mapMgr.UnregisterMap(1);
            }
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

            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PreEngine, frameTime);
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
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PostEngine, frameTime);

            SendGameStateUpdate();
            IoCManager.Resolve<IConsoleManager>().Update();
        }

        private void SendGameStateUpdate()
        {
            //Create a new GameState object
            var stateManager = IoCManager.Resolve<IGameStateManager>();
            var state = CreateGameState();
            stateManager.Add(state.Sequence, state);

            var netMan = IoCManager.Resolve<IServerNetManager>();
            var connections = netMan.Channels;
            if (!connections.Any())
            {
                //No clients -- don't send state
                stateManager.CullAll();
                return;
            }

            var playerMan = IoCManager.Resolve<IPlayerManager>();

            foreach (var c in connections)
            {
                var session = playerMan.GetSessionByChannel(c);
                if (session != null && (session.Status == SessionStatus.InGame || session.Status == SessionStatus.InLobby))
                    SendConnectionGameStateUpdate(c, state);
            }

            stateManager.Cull();
        }

        private void SendConnectionGameStateUpdate(INetChannel c, GameState state)
        {
            var netMan = IoCManager.Resolve<IServerNetManager>();
            if (c.Connection.Status != NetConnectionStatus.Connected)
            {
                return;
            }

            var session = IoCManager.Resolve<IPlayerManager>().GetSessionByChannel(c);
            if (session == null || session.Status != SessionStatus.InGame && session.Status != SessionStatus.InLobby)
            {
                return;
            }

            var stateManager = IoCManager.Resolve<IGameStateManager>();

            if (stateManager.GetLastStateAcked(c) == 0)
            {
                MsgFullState fullStateMessage = netMan.CreateNetMessage<MsgFullState>();
                fullStateMessage.State = state;

                netMan.ServerSendMessage(fullStateMessage, c);
            }
            else
            {
                MsgStateUpdate stateUpdateMessage = netMan.CreateNetMessage<MsgStateUpdate>();
                stateUpdateMessage.StateDelta = stateManager.GetDelta(c, _time.CurTick);

                netMan.ServerSendMessage(stateUpdateMessage, c);
            }
        }

        private GameState CreateGameState()
        {
            var state = new GameState(_time.CurTick);
            if (_entities != null)
                state.EntityStates = _entities.GetEntityStates();
            state.PlayerStates = IoCManager.Resolve<IPlayerManager>().GetPlayerStates();
            return state;
        }

        #region MessageProcessing

        private void HandleWelcomeMessageReq(NetMessage message)
        {
            var session = IoCManager.Resolve<IPlayerManager>().GetSessionByChannel(message.MsgChannel);
            session.Name = ((MsgServerInfoReq) message).PlayerName;

            var net = IoCManager.Resolve<IServerNetManager>();
            var netMsg = message.MsgChannel.CreateNetMessage<MsgServerInfo>();

            netMsg.ServerName = ServerName;
            netMsg.ServerPort = net.Port;
            netMsg.ServerWelcomeMessage = Motd;
            netMsg.ServerMaxPlayers = MaxPlayers;
            netMsg.ServerMapName = MapName;
            netMsg.GameMode = IoCManager.Resolve<IRoundManager>().CurrentGameMode.Name;
            netMsg.ServerPlayerCount = IoCManager.Resolve<IPlayerManager>().PlayerCount;
            netMsg.PlayerIndex = session.Index;

            message.MsgChannel.SendMessage(netMsg);

            
        }

        /// <summary>
        /// Player session is fully built, player is an active member of the server. Player is prepaired to start 
        /// receiving states when they join the lobby.
        /// </summary>
        /// <param name="session">Fully built session</param>
        public void PlayerJoinedServer(IPlayerSession session)
        {
            //TODO: There should be a way to notify the content

            // send the player to the lobby screen
            session.JoinLobby();
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

        private void HandlePlayerListReq(NetMessage message)
        {
            var channel = message.MsgChannel;
            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            var players = plyMgr.GetAllPlayers().ToArray();
            var netMsg = channel.CreateNetMessage<MsgPlayerList>();

            var list = new List<PlayerState>();
            foreach (var client in players)
            {
                if(client == null)
                    continue;

                var info = new PlayerState
                {
                    Index = client.Index,
                    Uuid = client.ConnectedClient.ConnectionId,
                    Name = client.Name,
                    Status = client.Status,
                    Ping = client.ConnectedClient.Ping
                };
                list.Add(info);
            }
            netMsg.Plyrs = list;
            netMsg.PlyCount = (byte) list.Count;

            channel.SendMessage(netMsg);

            // client session is complete
            var session = plyMgr.GetSessionByChannel(channel);
            PlayerJoinedServer(session);
        }

        private static void HandleClientGreet(MsgClGreet msg)
        {
            var p = IoCManager.Resolve<IPlayerManager>().GetSessionByChannel(msg.MsgChannel);

            var fixedName = msg.PlyName.Trim();
            if (fixedName.Length < 3)
                fixedName = $"Player {p.Index}";

            p.SetName(fixedName);
        }

        private static void HandleStateAck(MsgStateAck msg)
        {
            IoCManager.Resolve<IGameStateManager>().Ack(msg.MsgChannel.ConnectionId, msg.Sequence);
        }

        // The size of the map being sent is almost exactly 1 byte per tile.
        // The default 30x30 map is 900 bytes, a 100x100 one is 10,000 bytes (10kb).
        private static void SendMap(INetChannel client)
        {
            // Send Tiles
            IoCManager.Resolve<IMapManager>().SendMap(client);

            // TODO: Lets also send them all the items and mobs.

            // TODO: Send atmos state to player

            // Todo: Preempt this with the lobby.
            IoCManager.Resolve<IRoundManager>().SpawnPlayer(
                IoCManager.Resolve<IPlayerManager>().GetSessionByChannel(client)); //SPAWN PLAYER
        }

        #endregion MessageProcessing
    }
}
