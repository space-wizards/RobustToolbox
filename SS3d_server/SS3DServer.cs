using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SS13_Server.Modules;
using SS13_Server.Modules.Client;
using ServerServices;
using SS13_Server.Modules.Chat;
using Lidgren.Network;
using SS13_Shared;
using ServerServices.Configuration;
using ServerServices.Map;
using SS13_Server.Modules.Gamemodes;
using SGO;
using ServerInterfaces;
using ServerServices.MessageLogging;

namespace SS13_Server
{
    public class SS13Server
    {
        private readonly NetPeerConfiguration _netConfig = new NetPeerConfiguration("SS13_NetTag");
        public Dictionary<NetConnection, Client> ClientList = new Dictionary<NetConnection, Client>();
        public Map Map;
        public ChatManager ChatManager;
        public EntityManager EntityManager;
        public PlayerManager PlayerManager;
        public RunLevel Runlevel {get; private set;}

        //SAVE THIS SOMEWHERE ELSE
        private const int GameCountdown = 15;
        private DateTime _startAt;
        private int _lastAnnounced;
        private bool _active;

        public enum RunLevel
        {
            Init,
            Lobby,
            Game
        }

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

        public DateTime Time;   // The server current frame time
        
        #region Server Settings
        private int _serverPort = 1212;
        private string _serverName = "SS13 Server";
        private string _serverMapName = "SavedMap";
        private string _serverWelcomeMessage = "Welcome to the server!";
        private int _serverMaxPlayers = 32;
        private GameType _gameType = GameType.Game;

        public int FramePeriod = 33; // The time (in milliseconds) between server frames
        public DateTime LastUpdate;

        public float ServerRate     // desired server framerate in frames per second,  backed by framePeriod
        {
            get { return 1000.0f / FramePeriod; }
            set { FramePeriod = (int)(1000.0f / value); }
        }

        public SS13Server()
        {
            Runlevel = RunLevel.Init;
            _singleton = this;

            ServiceManager.Singleton.AddService(new ConfigManager("./config.xml"));
            LogManager.Initialize(ServiceManager.Singleton.Resolve<IConfigManager>().LogPath);
        }
        #endregion


        public void LoadSettings()
        {
            var cfgmgr = ServiceManager.Singleton.Resolve<IConfigManager>();
            _serverPort = cfgmgr.Port;
            _serverName = cfgmgr.ServerName;
            FramePeriod = cfgmgr.FramePeriod;
            _serverMapName = cfgmgr.ServerMapName;
            _serverMaxPlayers = cfgmgr.ServerMaxPlayers;
            _gameType = cfgmgr.GameType;
            _serverWelcomeMessage = cfgmgr.ServerWelcomeMessage;
            LogManager.Log("Port: " + _serverPort);
            LogManager.Log("Name: " + _serverName);
            LogManager.Log("Rate: " + (int)ServerRate + " (" + FramePeriod + " ms)");
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
                Map = new Map();
                ServiceManager.Singleton.AddService(Map);
                Map.InitMap(_serverMapName);

                EntityManager = new EntityManager(SS13NetServer.Singleton);
                //playerManager = new PlayerManager();

                RoundManager.Singleton.CurrentGameMode.StartGame();
            }
        }

        public bool Start()
        {
            //try
            //{
                Time = DateTime.Now;
                //LoadDataFile(dataFilename);
                LoadSettings();

                if (JobHandler.Singleton.LoadDefinitionsFromFile("JobDefinitions.xml"))
                {
                    LogManager.Log("Job Definitions File not found.", LogLevel.Fatal);
                    Environment.Exit(1);
                }
                else LogManager.Log("Job Definitions Found. " + JobHandler.Singleton.JobDefinitions.Count + " Jobs loaded.");

                BanlistMgr.Singleton.Initialize("BanList.xml");

                _netConfig.Port = _serverPort;
                var netServer = new SS13NetServer(_netConfig);
                ServiceManager.Singleton.AddService(netServer);
                SS13NetServer.Singleton.Start();

                ChatManager = new ChatManager();
                ServiceManager.Singleton.AddService(ChatManager);
                ServiceManager.Singleton.AddService(new MessageLogger(ServiceManager.Singleton.Resolve<IConfigManager>()));
                PlayerManager = new PlayerManager();

                CraftingManager.Singleton.Initialize("CraftingRecipes.xml", netServer, PlayerManager);

                StartLobby();
                StartGame();
                
                _active = true;
                return false;
            //}
            //catch (Lidgren.Network.NetException e)
            //{
            //    LogManager.Log(e.ToString(), LogLevel.Error);
            //    active = false;
            //    return true;
            //}
            //catch (Exception e)
            //{
            //    LogManager.Log(e.ToString(), LogLevel.Error);
            //    active = false;
            //    return true;
            //}
        }

        public void StartLobby()
        {
            RoundManager.Singleton.Initialize(new Gamemode()); //Load Type from config or something.
            InitModules();
        }

        public void DisposeForRestart()
        {
            EntityManager.Shutdown();
            EntityManager = null;
            Map.Shutdown();
            Map = null; //Implement proper disposal.
            GC.Collect();
        }

        public void Restart()
        {
            LogManager.Log("Restarting Server...");
            foreach (var curr in PlayerManager.playerSessions.Values)
                curr.JoinLobby();
            DisposeForRestart();
            StartLobby();
        }

        public void StartGame()
        {
            InitModules(RunLevel.Game);
            PlayerManager.SendJoinGameToAll();
        }

        #region server mainloop
        
        // The main server loop
        public void MainLoop()
        {
            while (Active)
            {
                try
                {
                    FrameStart();
                    ProcessPackets();
                    Update(FramePeriod);
                    CraftingManager.Singleton.Update();
                    var sleepTime = Time.AddMilliseconds(FramePeriod) - DateTime.Now;

                    if (sleepTime.TotalMilliseconds > 0)
                        Thread.Sleep(sleepTime);
                    //else
                    //    Console.WriteLine("Server slow by " + sleepTime.TotalMilliseconds);
                }
                catch (Exception e)
                {
                    LogManager.Log(e.ToString(), LogLevel.Error);
                    _active = false;
                }
            }
        }
        

        // called at the start of each server frame
        public void FrameStart()
        {
            Time = DateTime.Now;
        }

        public void ProcessPackets()
        {
            try
            {
                NetIncomingMessage msg;
                while ((msg = SS13NetServer.Singleton.ReadMessage()) != null)
                {
                    Console.Title = SS13NetServer.Singleton.Statistics.SentBytes + " " + SS13NetServer.Singleton.Statistics.ReceivedBytes;

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
                    SS13NetServer.Singleton.Recycle(msg);
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
                    Map.UpdateAtmos();
                    RoundManager.Singleton.CurrentGameMode.Update();
                }
            }
            else if (Runlevel == RunLevel.Lobby)
            {
                TimeSpan countdown = _startAt.Subtract(DateTime.Now);
                if (_lastAnnounced != countdown.Seconds)
                {
                    _lastAnnounced = countdown.Seconds;
                    ChatManager.SendChatMessage(ChatChannel.Server, "Starting in " + _lastAnnounced + " seconds...", "", 0);
                }
                if (countdown.Seconds <= 0)
                {
                    StartGame();
                }
            }
            LastUpdate = Time;
        }
        #endregion

        public bool Active
        {
            get { return _active; }
        }

        public void ShutDown()
        {

        }
        
        public void HandleConnectionApproval(NetConnection sender)
        {
            ClientList.Add(sender, new Client(sender));
        }

        public void SendWelcomeInfo(NetConnection connection)
        {
            NetOutgoingMessage welcomeMessage = SS13NetServer.Singleton.CreateMessage();
            welcomeMessage.Write((byte)NetMessage.WelcomeMessage);
            welcomeMessage.Write(_serverName);
            welcomeMessage.Write(_serverPort);
            welcomeMessage.Write(_serverWelcomeMessage);
            welcomeMessage.Write(_serverMaxPlayers);
            welcomeMessage.Write(_serverMapName);
            welcomeMessage.Write(RoundManager.Singleton.CurrentGameMode.Name);
            SS13NetServer.Singleton.SendMessage(welcomeMessage, connection, NetDeliveryMethod.ReliableOrdered);
            SendNewPlayerCount();
        }

        public void SendNewPlayerCount()
        {
            NetOutgoingMessage playercountMessage = SS13NetServer.Singleton.CreateMessage();
            playercountMessage.Write((byte)NetMessage.PlayerCount);
            playercountMessage.Write((byte)ClientList.Count);
            foreach (NetConnection conn in ClientList.Keys) //Why is this sent to everyone?
            {
                SS13NetServer.Singleton.SendMessage(playercountMessage, conn, NetDeliveryMethod.ReliableOrdered);
            }
        }

        public void SendPlayerList(NetConnection connection)
        {
            NetOutgoingMessage playerListMessage = SS13NetServer.Singleton.CreateMessage();
            playerListMessage.Write((byte)NetMessage.PlayerList);
            playerListMessage.Write((byte)ClientList.Count);

            foreach (NetConnection conn in ClientList.Keys)
            {
                PlayerSession plrSession = PlayerManager.GetSessionByConnection(conn);
                playerListMessage.Write(plrSession.name);
                playerListMessage.Write((byte)plrSession.status);
                playerListMessage.Write(ClientList[conn].netConnection.AverageRoundtripTime);
            }
            SS13NetServer.Singleton.SendMessage(playerListMessage, connection, NetDeliveryMethod.ReliableOrdered);
        }

        public void HandleStatusChanged(NetIncomingMessage msg)
        {
            var sender = msg.SenderConnection;
            var senderIp = sender.RemoteEndpoint.Address.ToString();
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
                    if (!BanlistMgr.Singleton.IsBanned(sender.RemoteEndpoint.Address.ToString()))
                    {
                        HandleConnectionApproval(sender);
                        PlayerManager.NewSession(sender); // TODO move this to somewhere that makes more sense.
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
                    PlayerManager.EndSession(sender);
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
                    CraftingManager.Singleton.HandleNetMessage(msg);
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
                case NetMessage.SendMap:
                    SendMap(msg.SenderConnection);
                    break;
                case NetMessage.PlayerList:
                    SendPlayerList(msg.SenderConnection);
                    break;
                case NetMessage.ClientName:
                    HandleClientName(msg);
                    break;
                case NetMessage.ChatMessage:
                    ChatManager.HandleNetMessage(msg);
                    break;
                case NetMessage.PlayerSessionMessage:
                    PlayerManager.HandleNetworkMessage(msg);
                    break;
                case NetMessage.MapMessage:
                    Map.HandleNetworkMessage(msg);
                    break;
                case NetMessage.JobList:
                    HandleJobListRequest(msg);
                    break;
                case NetMessage.PlacementManagerMessage:
                    PlacementManager.Singleton.HandleNetMessage(msg);
                    break;
                case NetMessage.EntityMessage:
                    EntityManager.HandleEntityNetworkMessage(msg);
                    break;
                case NetMessage.EntityManagerMessage:
                    EntityManager.HandleNetworkMessage(msg);
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
            }
        
        }

        public void HandleAdminMessage(NetMessage adminMsgType, NetIncomingMessage messageBody)
        {
            switch (adminMsgType)
            {
                case NetMessage.RequestEntityDeletion:
                    var entId = messageBody.ReadInt32();
                    if (PlayerManager.GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin || true) //TEMPORARY. REMOVE THE 'TRUE' LATER ON. !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    {
                        var delEnt = EntityManager.GetEntity(entId);
                        if (delEnt != null) EntityManager.DeleteEntity(delEnt);
                    }
                    break;
                case NetMessage.RequestAdminLogin:
                    var password = messageBody.ReadString();
                    if (password == ServiceManager.Singleton.Resolve<IConfigManager>().AdminPassword)
                    {
                        LogManager.Log("Admin login: " + messageBody.SenderConnection.RemoteEndpoint.Address);
                        PlayerManager.GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin = true;
                    }
                    else
                        LogManager.Log("Failed Admin login: " + messageBody.SenderConnection.RemoteEndpoint.Address + " -> ' " + password + " '");
                    break;
                case NetMessage.RequestAdminPlayerlist:
                    if (PlayerManager.GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin)
                    {
                        var adminPlayerListMessage = SS13NetServer.Singleton.CreateMessage();
                        adminPlayerListMessage.Write((byte)NetMessage.RequestAdminPlayerlist);
                        adminPlayerListMessage.Write((byte)ClientList.Count);
                        foreach (var plrSession in ClientList.Keys.Select(conn => PlayerManager.GetSessionByConnection(conn)))
                        {
                            adminPlayerListMessage.Write(plrSession.name);
                            adminPlayerListMessage.Write((byte)plrSession.status);
                            adminPlayerListMessage.Write(plrSession.assignedJob.Name);
                            adminPlayerListMessage.Write(plrSession.connectedClient.RemoteEndpoint.Address.ToString());
                            adminPlayerListMessage.Write(plrSession.adminPermissions.isAdmin);
                        }
                        SS13NetServer.Singleton.SendMessage(adminPlayerListMessage, messageBody.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    }
                    else
                    {
                        var loginMessage = SS13NetServer.Singleton.CreateMessage();
                        loginMessage.Write((byte)NetMessage.RequestAdminLogin);
                        SS13NetServer.Singleton.SendMessage(loginMessage, messageBody.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    }
                    break;
                case NetMessage.RequestAdminKick:
                    if (PlayerManager.GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin)
                    {
                        var ipKick = messageBody.ReadString();
                        var kickSession = PlayerManager.GetSessionByIp(ipKick);
                        if (kickSession != null)
                        {
                            PlayerManager.EndSession(kickSession.connectedClient);
                            kickSession.connectedClient.Disconnect("Kicked by Administrator.");
                        }
                    }
                    break;
                case NetMessage.RequestAdminBan:
                    if (PlayerManager.GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin)
                    {
                        var ipBan = messageBody.ReadString();
                        var banSession = PlayerManager.GetSessionByIp(ipBan);
                        if (banSession != null)
                        {
                            if (BanlistMgr.Singleton.IsBanned(ipBan)) return;
                            BanlistMgr.Singleton.AddBan(ipBan, "No reason specified.");
                            PlayerManager.EndSession(banSession.connectedClient);
                            banSession.connectedClient.Disconnect("Banned by Administrator.");
                        }
                    }
                    break;
                case NetMessage.RequestBanList:
                    if (PlayerManager.GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin)
                    {
                        var banListMessage = SS13NetServer.Singleton.CreateMessage();
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
                        SS13NetServer.Singleton.SendMessage(banListMessage, messageBody.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    }
                    break;
                case NetMessage.RequestAdminUnBan:
                    if (PlayerManager.GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin)
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

            var session =  PlayerManager.GetSessionByConnection(msg.SenderConnection);
            session.assignedJob = pickedJob;

            var jobSelectedMessage = SS13NetServer.Singleton.CreateMessage();
            jobSelectedMessage.Write((byte)NetMessage.JobSelected);
            jobSelectedMessage.Write(pickedJob.Name);
            SS13NetServer.Singleton.SendMessage(jobSelectedMessage, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void HandleJobListRequest(NetIncomingMessage msg)
        {
            //var p = PlayerManager.GetSessionByConnection(msg.SenderConnection);
            var jobListMessage = SS13NetServer.Singleton.CreateMessage();
            jobListMessage.Write((byte)NetMessage.JobList);
            jobListMessage.Write(JobHandler.Singleton.GetDefinitionsString());
            SS13NetServer.Singleton.SendMessage(jobListMessage, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void HandleClientName(NetIncomingMessage msg)
        {
            var name = msg.ReadString();
            ClientList[msg.SenderConnection].SetName(name);
            var fixedname = name.Trim();
            if (fixedname.Length < 3)
                fixedname = "Player";
            var p = PlayerManager.GetSessionByConnection(msg.SenderConnection);
            p.SetName(fixedname);
       }

        // The size of the map being sent is almost exaclty 1 byte per tile.
        // The default 30x30 map is 900 bytes, a 100x100 one is 10,000 bytes (10kb).
        public void SendMap(NetConnection connection)
        {
            LogManager.Log(connection.RemoteEndpoint.Address + ": Sending map");
            var mapMessage = SS13NetServer.Singleton.CreateMessage();
            mapMessage.Write((byte)NetMessage.SendMap);

            var mapWidth = Map.GetMapWidth();
            var mapHeight = Map.GetMapHeight();

            mapMessage.Write(mapWidth);
            mapMessage.Write(mapHeight);

            for (var x = 0; x < mapWidth; x++)
            {
                for (var y = 0; y < mapHeight; y++)
                {
                    var t = Map.GetTileAt(x, y);
                    mapMessage.Write((byte)t.tileType);
                    mapMessage.Write((byte)t.tileState);
                }
            }

            SS13NetServer.Singleton.SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
            LogManager.Log(connection.RemoteEndpoint.Address + ": Sending map finished with message size: " + mapMessage.LengthBytes + " bytes");

            // Lets also send them all the items and mobs.
            EntityManager.Singleton.SendEntities(connection);
            //playerManager.SpawnPlayerMob(playerManager.GetSessionByConnection(connection));
            //Send atmos state to player
            Map.SendAtmosStateTo(connection);
            //Todo: Preempt this with the lobby.
            RoundManager.Singleton.SpawnPlayer(PlayerManager.GetSessionByConnection(connection)); //SPAWN PLAYER
        }

        public void SendChangeTile(int x, int z, TileType newType)
        {
            NetOutgoingMessage tileMessage = SS13NetServer.Singleton.CreateMessage();
            //tileMessage.Write((byte)NetMessage.ChangeTile);
            tileMessage.Write(x);
            tileMessage.Write(z);
            tileMessage.Write((byte)newType);
            foreach(var connection in ClientList.Keys)
            {
                SS13NetServer.Singleton.SendMessage(tileMessage, connection, NetDeliveryMethod.ReliableOrdered);
                LogManager.Log(connection.RemoteEndpoint.Address + ": Tile Change Being Sent", LogLevel.Debug);
            }
        }

        /*public void SendMessageToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            if (message == null)
            {
                return;
            }
            int i = clientList.Count;
            //Console.WriteLine("Sending to all ("+i+") with size: " + message.LengthBits + " bytes");

            SS13NetServer.Singleton.SendMessage(message, SS13NetServer.Singleton.Connections, method, 0);
        }*/

        /*public void SendMessageTo(NetOutgoingMessage message, NetConnection connection, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            if (message == null || connection == null)
            {
                return;
            }
            LogManager.Log("Sending to one with size: " + message.LengthBytes + " bytes", LogLevel.Debug);
            SS13NetServer.Singleton.SendMessage(message, connection, method);
        }*/
    }
}
