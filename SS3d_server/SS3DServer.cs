using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SS13_Server.Modules;
using SS13_Server.Modules.Client;
using SS13_Shared.GameStates;
using ServerInterfaces.ServerConsole;
using ServerInterfaces.GameState;
using ServerInterfaces.Serialization;
using ServerServices;
using Lidgren.Network;
using SS13_Shared;
using ServerServices.Map;
using SGO;
using ServerInterfaces;
using ServerServices.Log;
using SS13.IoC;
using ServerInterfaces.Configuration;
using ServerInterfaces.Network;
using ServerInterfaces.Chat;
using ServerInterfaces.GameObject;
using ServerInterfaces.Player;
using SS13_Shared.ServerEnums;
using ServerInterfaces.Crafting;
using ServerInterfaces.Round;
using ServerServices.Round;
using ServerInterfaces.Map;
using ServerInterfaces.Placement;
using ServerInterfaces.Atmos;
using SS13_Shared;

namespace SS13_Server
{
    public class SS13Server: ISS13Server
    {
        public Dictionary<NetConnection, IClient> ClientList = new Dictionary<NetConnection, IClient>();
        public IEntityManager EntityManager { get; private set; }
        public RunLevel Runlevel {get; private set;}
        
        public string ConsoleTitle { get; private set; }
        private List<float> frameTimes = new List<float>();


        // The servers current frame time
        public DateTime Time;
        public Stopwatch stopWatch = new Stopwatch();
        private float millisecondsPerTick; //

        //Threading!
        private bool updateThreadRunning = false;
        private object updateThreadLock = new Object();
        private Timer mainLoopTimer;

        //SAVE THIS SOMEWHERE ELSE
        private const int GameCountdown = 15;
        private DateTime _startAt;
        private int _lastAnnounced;
        private bool _active;

        // State update vars
        private int updateRate = 20; //20 updates per second
        private uint _oldestAckedState = 0;
        private uint _lastState = 0;
        private DateTime _lastStateTime = DateTime.Now;
        
        private static SS13Server _singleton;
        public static SS13Server Singleton
        {
            get
            {
                if (_singleton == null)
                    throw new TypeInitializationException("Singleton not initialized.", null);
                return _singleton;
            }
        }
        
        #region Server Settings
        private int _serverPort = 1212;
        private string _serverName = "SS13 Server";
        private string _serverMapName = "SavedMap";
        private string _serverWelcomeMessage = "Welcome to the server!";
        private int _serverMaxPlayers = 32;
        private GameType _gameType = GameType.Game;

        public DateTime LastUpdate;
        private int lastSentBytes = 0;
        private int lastRecievedBytes = 0;
        private DateTime lastBytesUpdate = DateTime.Now;

        public float ServerRate     // desired server frame (tick) time in milliseconds
        {
            get { return 1000.0f / TickRate; }
        }

        public float TickRate // desired server frames (ticks) per second
        {
            get { return IoCManager.Resolve<IConfigurationManager>().TickRate; }
        }

        public SS13Server()
        {
            IoCManager.Resolve<ISS13Server>().SetServerInstance(this);
            millisecondsPerTick = Stopwatch.Frequency / 1000; //Ticks per second dividied by 1000 = ticks per millisecond
         
            //Init serializer
            var serializer = IoCManager.Resolve<ISS13Serializer>();

            Runlevel = RunLevel.Init;
            _singleton = this;

            IoCManager.Resolve<IConfigurationManager>().Initialize("./config.xml");
            LogManager.Initialize(IoCManager.Resolve<IConfigurationManager>().LogPath, IoCManager.Resolve<IConfigurationManager>().LogLevel);
        }
        #endregion

        public void LoadSettings()
        {
            var cfgmgr = IoCManager.Resolve<IConfigurationManager>();
            _serverPort = cfgmgr.Port;
            _serverName = cfgmgr.ServerName;
            _serverMapName = cfgmgr.ServerMapName;
            _serverMaxPlayers = cfgmgr.ServerMaxPlayers;
            _gameType = cfgmgr.GameType;
            _serverWelcomeMessage = cfgmgr.ServerWelcomeMessage;
            LogManager.Log("Port: " + _serverPort);
            LogManager.Log("Name: " + _serverName);
            LogManager.Log("TickRate: " + TickRate + "(" + ServerRate + "ms)");
            LogManager.Log("Map: " + _serverMapName);
            LogManager.Log("Max players: " + _serverMaxPlayers);
            LogManager.Log("Game type: " + _gameType);
            LogManager.Log("Welcome message: " + _serverWelcomeMessage);
        }

        /// <summary>
        /// Controls what modules are running.
        /// </summary>
        /// <param name="runlevel"></param>
        public void InitModules(RunLevel runlevel = RunLevel.Lobby)
        {
            if (runlevel == Runlevel)
                return;

            Runlevel = runlevel;
            if (Runlevel == RunLevel.Lobby)
            {
                _startAt = DateTime.Now.AddSeconds(GameCountdown);
            }
            else if (Runlevel == RunLevel.Game)
            {
                IoCManager.Resolve<IMapManager>().InitMap(_serverMapName);

                IoCManager.Resolve<IAtmosManager>().InitializeGasCells();

                EntityManager = new EntityManager(IoCManager.Resolve<ISS13NetServer>());

                IoCManager.Resolve<IRoundManager>().CurrentGameMode.StartGame();
            }
        }

        public bool Start()
        {
            Time = DateTime.Now;

            LoadSettings();

            if (JobHandler.Singleton.LoadDefinitionsFromFile("JobDefinitions.xml"))
            {
                LogManager.Log("Job Definitions File not found.", LogLevel.Fatal);
                Environment.Exit(1);
            }
            else LogManager.Log("Job Definitions Found. " + JobHandler.Singleton.JobDefinitions.Count + " Jobs loaded.");

            BanlistMgr.Singleton.Initialize("BanList.xml");

            IoCManager.Resolve<ISS13NetServer>().Start();
            IoCManager.Resolve<IChatManager>().Initialize(this);
            IoCManager.Resolve<IPlayerManager>().Initialize(this);
            IoCManager.Resolve<ICraftingManager>().Initialize("CraftingRecipes.xml", this);
            IoCManager.Resolve<IPlacementManager>().Initialize(this);

            StartLobby();
            StartGame();
                
            _active = true;
            return false;
        }

        public void StartLobby()
        {
            IoCManager.Resolve<IRoundManager>().Initialize(new Gamemode(this));
            InitModules(RunLevel.Lobby);
        }

        public void StartGame()
        {
            InitModules(RunLevel.Game);
            IoCManager.Resolve<IPlayerManager>().SendJoinGameToAll();
        }

        public void DisposeForRestart()
        {
            IoCManager.Resolve<IPlayerManager>().DetachAll();
            EntityManager.Shutdown();
            EntityManager = null;
            IoCManager.Resolve<IMapManager>().Shutdown();
            GC.Collect();
        }

        public void Restart()
        {
            LogManager.Log("Restarting Server...");
            IoCManager.Resolve<IPlayerManager>().SendJoinLobbyToAll();
            DisposeForRestart();
            StartLobby();
        }

        #region server mainloop
        
        // The main server loop
        public void MainLoop()
        {
            TimerCallback tcb = RunLoop;
            AutoResetEvent are = new AutoResetEvent(false);
            var due = (long)ServerRate;
            stopWatch.Start(); //Start the clock
            mainLoopTimer = new Timer(tcb, are, 0, due);
            are.WaitOne(-1);
        }

        public void RunLoop(Object stateInfo)
        {
            /*** DO NOT CHANGE ANYTHING IN THIS FUNCTION UNLESS YOU ARE SPOOGE OR YOU UNDERSTAND EVERYTHING ***/

            // This prevents more than one update thread running at once
            lock (updateThreadLock)
            {
                if (updateThreadRunning)
                {
                    return;
                }
                updateThreadRunning = true;
            }
            
            using (var autoReset = stateInfo as AutoResetEvent)
            {
                if (!Active)
                {
                    autoReset.Set();
                    return;
                }
            }

            // If the debugger is attached, let the errors dump through and break in the debugger
            if (Debugger.IsAttached)
            {
                DoMainLoopStuff(stateInfo);
            }
            else //Otherwise log them and crash out.
            {
                try
                {
                    DoMainLoopStuff(stateInfo);
                }
                catch (Exception e)
                {
                    LogManager.Log(e.ToString(), LogLevel.Error);
                    _active = false;
                }
            }

            // Release the update thread lock
            lock (updateThreadLock)
            {
                updateThreadRunning = false;
            }
        }

        private void DoMainLoopStuff(Object stateInfo)
        {
            float elapsedTime;
            
            elapsedTime = (stopWatch.ElapsedTicks / millisecondsPerTick); //Elapsed time in milliseconds since the last tick
                        stopWatch.Restart(); //Reset the stopwatch so we get elapsed time next time

            //Begin update time
            Time = DateTime.Now;
            ProcessPackets();

            Update(elapsedTime);

            IoCManager.Resolve<IConsoleManager>().Update();

            /*var updateElapsedTime = DateTime.Now - Time;
            var sleepTime = new TimeSpan((long)(10000 * (ServerRate - (float)updateElapsedTime.TotalMilliseconds)));
            //var sleepTime = Time.AddMilliseconds(ServerRate) - DateTime.Now;
            if (sleepTime.TotalMilliseconds > 0)
                Thread.Sleep(sleepTime);
            var totalElapsedTime = (DateTime.Now - Time).TotalMilliseconds;
            var rate = 1 / (totalElapsedTime / 1000);*/
            if (frameTimes.Count >= TickRate)
                frameTimes.RemoveAt(0);
            var rate = 1000 / elapsedTime;
            frameTimes.Add(rate);


            if ((DateTime.Now - lastBytesUpdate).TotalMilliseconds > 1000)
            {
                var netstats = UpdateBPS();
                Console.Title = "FPS: " + Math.Round(frameTimeAverage(), 2) + " | Netstats: " + netstats;
                lastBytesUpdate = DateTime.Now;
            }
        }

        private string UpdateBPS()
        {
            string BPS = "S: " + (IoCManager.Resolve<ISS13NetServer>().Statistics.SentBytes - lastSentBytes) / 1000f + "kB/s | R: " + (IoCManager.Resolve<ISS13NetServer>().Statistics.ReceivedBytes - lastRecievedBytes) / 1000f + "kB/s";
            lastSentBytes = IoCManager.Resolve<ISS13NetServer>().Statistics.SentBytes;
            lastRecievedBytes = IoCManager.Resolve<ISS13NetServer>().Statistics.ReceivedBytes;
            return BPS;
        }

        private float frameTimeAverage()
        {
            if(frameTimes.Count == 0)
                return 0;
            return frameTimes.Average(p => p);
        }

        public void ProcessPackets()
        {
            try
            {
                NetIncomingMessage msg;
                while ((msg = IoCManager.Resolve<ISS13NetServer>().ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                            LogManager.Log(msg.ReadString(), LogLevel.Debug);
                            break;

                        case NetIncomingMessageType.DebugMessage:
                            LogManager.Log(msg.ReadString(), LogLevel.Debug);
                            break;

                        case NetIncomingMessageType.WarningMessage:
                            LogManager.Log(msg.ReadString(), LogLevel.Warning);
                            break;

                        case NetIncomingMessageType.ErrorMessage:
                            LogManager.Log(msg.ReadString(), LogLevel.Error);
                            break;

                        case NetIncomingMessageType.Data:
                            if (ClientList.ContainsKey(msg.SenderConnection))
                            {
                                HandleData(msg);
                            }
                            break;

                        case NetIncomingMessageType.StatusChanged:
                            HandleStatusChanged(msg);
                            break;
                        default:
                            LogManager.Log("Unhandled type: " + msg.MessageType, LogLevel.Error);
                            break;
                    }
                    IoCManager.Resolve<ISS13NetServer>().Recycle(msg);
                }                
            }
            catch (Exception e)
            {
                LogManager.Log(e.ToString(), LogLevel.Error);
            }
        }

        public void Update(float framePeriod)
        {
            if (Runlevel == RunLevel.Game)
            {
                TimeSpan lastFrame = Time - LastUpdate;
                if (lastFrame.TotalMilliseconds > framePeriod)
                {
                    ComponentManager.Singleton.Update(framePeriod);
                    IoCManager.Resolve<IAtmosManager>().Update();
                    IoCManager.Resolve<IRoundManager>().CurrentGameMode.Update();
                    IoCManager.Resolve<ICraftingManager>().Update();
                }
                EntityManager.Update(framePeriod);
            }
            else if (Runlevel == RunLevel.Lobby)
            {
                TimeSpan countdown = _startAt.Subtract(DateTime.Now);
                if (_lastAnnounced != countdown.Seconds)
                {
                    _lastAnnounced = countdown.Seconds;
                    IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Server, "Starting in " + _lastAnnounced + " seconds...", "", 0);
                }
                if (countdown.Seconds <= 0)
                {
                    StartGame();
                }
            }
            LastUpdate = Time;
            SendGameStateUpdate();
        }

        public void SendGameStateUpdate()
        {
            //Obey the updates per second limit
            TimeSpan elapsed = Time - _lastStateTime;
            if (elapsed.TotalMilliseconds > (1000 / updateRate))
            {
                //Save last state time
                _lastStateTime = Time;
                //Create a new GameState object
                var stateManager = IoCManager.Resolve<IGameStateManager>();
                GameState state = new GameState(++_lastState);
                if(EntityManager != null)
                    state.EntityStates = EntityManager.GetEntityStates();
                state.PlayerStates = IoCManager.Resolve<IPlayerManager>().GetPlayerStates();
                stateManager.Add(state.Sequence, state);

                //LogManager.Log("Update " + _lastState + " sent.");
                var connections = IoCManager.Resolve<ISS13NetServer>().Connections;
                if (connections.Count == 0)
                { //No clients -- don't send state
                    _oldestAckedState = _lastState;
                    stateManager.Clear();
                }
                else
                {
                    foreach(var c in IoCManager.Resolve<ISS13NetServer>().Connections.Where(c => c.Status == NetConnectionStatus.Connected))
                    {
                        var session = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(c);
                        if (session == null || session.status != SessionStatus.InGame)
                            continue;
                        var stateMessage = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
                        var lastStateAcked = stateManager.GetLastStateAcked(c);
                        if(lastStateAcked == 0)
                        {
                            var length = state.WriteStateMessage(stateMessage);
                            //LogManager.Log("Full state of size " + length + " sent to " + c.RemoteUniqueIdentifier);
                        } 
                        else
                        {
                            stateMessage.Write((byte)NetMessage.StateUpdate);
                            var delta = stateManager.GetDelta(c, _lastState);
                            delta.WriteDelta(stateMessage);
                            //LogManager.Log("Delta of size " + delta.Size + " sent to " + c.RemoteUniqueIdentifier);
                        }

                        IoCManager.Resolve<ISS13NetServer>().SendMessage(stateMessage, c, NetDeliveryMethod.Unreliable);
                    }
                }
                stateManager.Cull();
            }
        }
        #endregion

        public bool Active
        {
            get { return _active; }
        }
        
        public void HandleConnectionApproval(NetConnection sender)
        {
            ClientList.Add(sender, new Client(sender));
        }

        public void SendWelcomeInfo(NetConnection connection)
        {
            NetOutgoingMessage welcomeMessage = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            welcomeMessage.Write((byte)NetMessage.WelcomeMessage);
            welcomeMessage.Write(_serverName);
            welcomeMessage.Write(_serverPort);
            welcomeMessage.Write(_serverWelcomeMessage);
            welcomeMessage.Write(_serverMaxPlayers);
            welcomeMessage.Write(_serverMapName);
            welcomeMessage.Write(IoCManager.Resolve<IRoundManager>().CurrentGameMode.Name);
            IoCManager.Resolve<ISS13NetServer>().SendMessage(welcomeMessage, connection, NetDeliveryMethod.ReliableOrdered);
            SendNewPlayerCount();
        }

        public void SendNewPlayerCount()
        {
            NetOutgoingMessage playercountMessage = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            playercountMessage.Write((byte)NetMessage.PlayerCount);
            playercountMessage.Write((byte)ClientList.Count);
            IoCManager.Resolve<ISS13NetServer>().SendToAll(playercountMessage);
        }

        public void SendPlayerList(NetConnection connection)
        {
            NetOutgoingMessage playerListMessage = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            playerListMessage.Write((byte)NetMessage.PlayerList);
            playerListMessage.Write((byte)ClientList.Count);

            foreach (NetConnection conn in ClientList.Keys)
            {
                IPlayerSession plrSession = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(conn);
                playerListMessage.Write(plrSession.name);
                playerListMessage.Write((byte)plrSession.status);
                playerListMessage.Write(ClientList[conn].NetConnection.AverageRoundtripTime);
            }
            IoCManager.Resolve<ISS13NetServer>().SendMessage(playerListMessage, connection, NetDeliveryMethod.ReliableOrdered);
        }

        public void HandleStatusChanged(NetIncomingMessage msg)
        {
            var sender = msg.SenderConnection;
            var senderIp = sender.RemoteEndPoint.Address.ToString();
            LogManager.Log(senderIp + ": Status change");

            switch (sender.Status)
            {
                case NetConnectionStatus.Connected:
                    LogManager.Log(senderIp + ": Connection request");
                    if (ClientList.ContainsKey(sender))
                    {
                        LogManager.Log(senderIp + ": Already connected", LogLevel.Error);
                        return;
                    }
                    if (!BanlistMgr.Singleton.IsBanned(sender.RemoteEndPoint.Address.ToString()))
                    {
                        HandleConnectionApproval(sender);
                        IoCManager.Resolve<IPlayerManager>().NewSession(sender); // TODO move this to somewhere that makes more sense.
                    }
                    else
                    {
                        //You're banned bro.
                        var ban = BanlistMgr.Singleton.GetBanByIp(senderIp);
                        sender.Disconnect("You have been banned from this Server." + Environment.NewLine + "Reason: " + ban.reason + Environment.NewLine + "Expires: " + (ban.tempBan ? ban.expiresAt.ToString("d/M/yyyy HH:mm:ss") : "Never"));
                        LogManager.Log(senderIp + ": Connection denied. User banned.");
                    }
                    break;
                case NetConnectionStatus.Disconnected:
                    LogManager.Log(senderIp + ": Disconnected");
                    IoCManager.Resolve<IPlayerManager>().EndSession(sender);
                    if (ClientList.ContainsKey(sender))
                    {
                        ClientList.Remove(sender);
                    }
                    break;
            }
        }

        /// <summary>
        /// Main method for routing incoming application network messages
        /// </summary>
        /// <param name="msg"></param>
        public void HandleData(NetIncomingMessage msg)
        {
            var messageType = (NetMessage)msg.ReadByte();
            switch (messageType)
            {
                case NetMessage.CraftMessage:
                    IoCManager.Resolve<ICraftingManager>().HandleNetMessage(msg);
                    break;
                case NetMessage.WelcomeMessage:
                    SendWelcomeInfo(msg.SenderConnection);
                    break;
                case NetMessage.RequestJob:
                    HandleJobRequest(msg);
                    break;
                case NetMessage.ForceRestart:
                    Restart();
                    break;
                case NetMessage.RequestMap:
                    SendMap(msg.SenderConnection);
                    break;
                case NetMessage.PlayerList:
                    SendPlayerList(msg.SenderConnection);
                    break;
                case NetMessage.ClientName:
                    HandleClientName(msg);
                    break;
                case NetMessage.ChatMessage:
                    IoCManager.Resolve<IChatManager>().HandleNetMessage(msg);
                    break;
                case NetMessage.PlayerSessionMessage:
                    IoCManager.Resolve<IPlayerManager>().HandleNetworkMessage(msg);
                    break;
                case NetMessage.MapMessage:
                    IoCManager.Resolve<IMapManager>().HandleNetworkMessage(msg);
                    break;
                case NetMessage.JobList:
                    HandleJobListRequest(msg);
                    break;
                case NetMessage.PlacementManagerMessage:
                    IoCManager.Resolve<IPlacementManager>().HandleNetMessage(msg);
                    break;
                case NetMessage.EntityMessage:
                    EntityManager.HandleEntityNetworkMessage(msg);
                    break;
                case NetMessage.RequestAdminLogin:
                    HandleAdminMessage(messageType, msg);
                    break;
                case NetMessage.RequestAdminPlayerlist:
                    HandleAdminMessage(messageType, msg);
                    break;
                case NetMessage.RequestAdminKick:
                    HandleAdminMessage(messageType, msg);
                    break;
                case NetMessage.RequestAdminBan:
                    HandleAdminMessage(messageType, msg);
                    break;
                case NetMessage.RequestAdminUnBan:
                    HandleAdminMessage(messageType, msg);
                    break;
                case NetMessage.RequestBanList:
                    HandleAdminMessage(messageType, msg);
                    break;
                case NetMessage.RequestEntityDeletion:
                    HandleAdminMessage(messageType, msg);
                    break;
                case NetMessage.StateAck:
                    HandleStateAck(msg);
                    break;
            }
        
        }

        public void HandleAdminMessage(NetMessage adminMsgType, NetIncomingMessage messageBody)
        {
            switch (adminMsgType)
            {
                case NetMessage.RequestEntityDeletion:
                    var entId = messageBody.ReadInt32();
                    if (IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin || true) //TEMPORARY. REMOVE THE 'TRUE' LATER ON. !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    {
                        var delEnt = EntityManager.GetEntity(entId);
                        if (delEnt != null) EntityManager.DeleteEntity(delEnt);
                    }
                    break;
                case NetMessage.RequestAdminLogin:
                    var password = messageBody.ReadString();
                    if (password == IoCManager.Resolve<IConfigurationManager>().AdminPassword)
                    {
                        LogManager.Log("Admin login: " + messageBody.SenderConnection.RemoteEndPoint.Address);
                        IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin = true;
                    }
                    else
                        LogManager.Log("Failed Admin login: " + messageBody.SenderConnection.RemoteEndPoint.Address + " -> ' " + password + " '");
                    break;
                case NetMessage.RequestAdminPlayerlist:
                    if (IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin)
                    {
                        var adminPlayerListMessage = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
                        adminPlayerListMessage.Write((byte)NetMessage.RequestAdminPlayerlist);
                        adminPlayerListMessage.Write((byte)ClientList.Count);
                        foreach (var plrSession in ClientList.Keys.Select(conn => IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(conn)))
                        {
                            adminPlayerListMessage.Write(plrSession.name);
                            adminPlayerListMessage.Write((byte)plrSession.status);
                            adminPlayerListMessage.Write(plrSession.assignedJob.Name);
                            adminPlayerListMessage.Write(plrSession.connectedClient.RemoteEndPoint.Address.ToString());
                            adminPlayerListMessage.Write(plrSession.adminPermissions.isAdmin);
                        }
                        IoCManager.Resolve<ISS13NetServer>().SendMessage(adminPlayerListMessage, messageBody.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    }
                    else
                    {
                        var loginMessage = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
                        loginMessage.Write((byte)NetMessage.RequestAdminLogin);
                        IoCManager.Resolve<ISS13NetServer>().SendMessage(loginMessage, messageBody.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    }
                    break;
                case NetMessage.RequestAdminKick:
                    if (IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin)
                    {
                        var ipKick = messageBody.ReadString();
                        var kickSession = IoCManager.Resolve<IPlayerManager>().GetSessionByIp(ipKick);
                        if (kickSession != null)
                        {
                            IoCManager.Resolve<IPlayerManager>().EndSession(kickSession.connectedClient);
                            kickSession.connectedClient.Disconnect("Kicked by Administrator.");
                        }
                    }
                    break;
                case NetMessage.RequestAdminBan:
                    if (IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin)
                    {
                        var ipBan = messageBody.ReadString();
                        var banSession = IoCManager.Resolve<IPlayerManager>().GetSessionByIp(ipBan);
                        if (banSession != null)
                        {
                            if (BanlistMgr.Singleton.IsBanned(ipBan)) return;
                            BanlistMgr.Singleton.AddBan(ipBan, "No reason specified.");
                            IoCManager.Resolve<IPlayerManager>().EndSession(banSession.connectedClient);
                            banSession.connectedClient.Disconnect("Banned by Administrator.");
                        }
                    }
                    break;
                case NetMessage.RequestBanList:
                    if (IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin)
                    {
                        var banListMessage = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
                        banListMessage.Write((byte)NetMessage.RequestBanList);
                        banListMessage.Write(BanlistMgr.Singleton.banlist.List.Count);
                        foreach (var t in BanlistMgr.Singleton.banlist.List)
                        {
                            banListMessage.Write(t.ip);
                            banListMessage.Write(t.reason);
                            banListMessage.Write(t.tempBan);
                            var compare = t.expiresAt.CompareTo(DateTime.Now);
                            var timeLeft = compare < 0 ? new TimeSpan(0) : t.expiresAt.Subtract(DateTime.Now);
                            var minutesLeft = (uint)Math.Truncate(timeLeft.TotalMinutes);
                            banListMessage.Write(minutesLeft);
                        }
                        IoCManager.Resolve<ISS13NetServer>().SendMessage(banListMessage, messageBody.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    }
                    break;
                case NetMessage.RequestAdminUnBan:
                    if (IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin)
                    {
                        var ip = messageBody.ReadString();
                        BanlistMgr.Singleton.RemoveBanByIp(ip);
                    }
                    break;
            }

        }

        public void HandleJobRequest(NetIncomingMessage msg)
        {
            string name = msg.ReadString();
            var pickedJob = (from JobDefinition def in JobHandler.Singleton.JobDefinitions
                             where def.Name == name
                             select def).First();

            if (pickedJob == null) return;

            var session = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection);
            session.assignedJob = pickedJob;

            var jobSelectedMessage = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            jobSelectedMessage.Write((byte)NetMessage.JobSelected);
            jobSelectedMessage.Write(pickedJob.Name);
            IoCManager.Resolve<ISS13NetServer>().SendMessage(jobSelectedMessage, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void HandleJobListRequest(NetIncomingMessage msg)
        {
            var jobListMessage = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            jobListMessage.Write((byte)NetMessage.JobList);
            jobListMessage.Write(JobHandler.Singleton.GetDefinitionsString());
            IoCManager.Resolve<ISS13NetServer>().SendMessage(jobListMessage, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void HandleClientName(NetIncomingMessage msg)
        {
            var name = msg.ReadString();
            ClientList[msg.SenderConnection].PlayerName = name;
            var fixedname = name.Trim();
            if (fixedname.Length < 3)
                fixedname = "Player";
            var p = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection);
            p.SetName(fixedname);
       }

        public void HandleStateAck(NetIncomingMessage msg)
        {
            var sequence = msg.ReadUInt32();
            IoCManager.Resolve<IGameStateManager>().Ack(msg.SenderConnection.RemoteUniqueIdentifier, sequence);
            //LogManager.Log("State Acked: " + sequence + " by client " + msg.SenderConnection.RemoteUniqueIdentifier + ".");
        }

        // The size of the map being sent is almost exaclty 1 byte per tile.
        // The default 30x30 map is 900 bytes, a 100x100 one is 10,000 bytes (10kb).
        public void SendMap(NetConnection connection)
        {
            // Send Tiles
            IoCManager.Resolve<IMapManager>().SendMap(connection);

            // Lets also send them all the items and mobs.
            EntityManager.SendEntities(connection);

            // Send atmos state to player
            IoCManager.Resolve<IAtmosManager>().SendAtmosStateTo(connection);

            // Todo: Preempt this with the lobby.
            IoCManager.Resolve<IRoundManager>().SpawnPlayer(IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(connection)); //SPAWN PLAYER
        }

        public void SendChangeTile(int x, int z, string newType)
        {
            NetOutgoingMessage tileMessage = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            var mapMgr = (MapManager)IoCManager.Resolve<IMapManager>();
            //tileMessage.Write((byte)NetMessage.ChangeTile);
            tileMessage.Write(x);
            tileMessage.Write(z);
            tileMessage.Write(mapMgr.GetTileIndex(newType));
            foreach(var connection in ClientList.Keys)
            {
                IoCManager.Resolve<ISS13NetServer>().SendMessage(tileMessage, connection, NetDeliveryMethod.ReliableOrdered);
                LogManager.Log(connection.RemoteEndPoint.Address + ": Tile Change Being Sent", LogLevel.Debug);
            }
        }

        public IClient GetClient(NetConnection clientConnection)
        {
            return ClientList[clientConnection];
        }

        public void SaveMap()
        {
            IoCManager.Resolve<IMapManager>().SaveMap();
        }

        public void SaveEntities()
        {
            EntityManager.SaveEntities();
        }

        public IMapManager GetMap()
        {
            return IoCManager.Resolve<IMapManager>();
        }




        public void SetServerInstance(ISS13Server server)
        {} //Bogus -- this is some shit for the surrogate class in ServerServices.
    }
}
