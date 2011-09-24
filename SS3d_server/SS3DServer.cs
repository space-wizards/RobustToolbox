using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using SS3D_Server.Modules;
using SS3D_Server.Modules.Client;
using SS3D_Server.Modules.Map;
using SS3D_Server.Modules.Chat;
using SS3D_Server.Atom;
using System.IO.Compression;
using Lidgren.Network;
using SS3D_shared;

using SS3D_Server.Modules.Gamemodes;
using SGO;

namespace SS3D_Server
{
    public class SS3DServer
    {
        private NetPeerConfiguration netConfig = new NetPeerConfiguration("SS3D_NetTag");
        public Dictionary<NetConnection, Client> clientList = new Dictionary<NetConnection, Client>();
        public Map map;
        public ChatManager chatManager;
        public AtomManager atomManager;
        public EntityManager entityManager;
        public PlayerManager playerManager;
        public RunLevel runlevel {get;private set;}

        //SAVE THIS SOMEWHERE ELSE
        private const int game_countdown = 15;
        private DateTime StartAt;
        private int lastAnnounced = 0;

        public enum RunLevel
        {
            Init,
            Lobby,
            Game
        }
        bool active = false;

        private static SS3DServer singleton;
        public static SS3DServer Singleton
        {
            get
            {
                if (singleton == null)
                    throw new TypeInitializationException("Singleton not initialized.", null);
                return singleton;
            }
        }

        public DateTime time;   // The server current frame time
        
        #region Server Settings
        int serverPort = 1212;
        string serverName = "SS3D Server";
        string serverMapName = "SavedMap";
        string serverWelcomeMessage = "Welcome to the server!";
        int serverMaxPlayers = 32;
        GameType gameType = GameType.Game;

        public int framePeriod = 33; // The time (in milliseconds) between server frames
        public DateTime lastUpdate;

        public float serverRate     // desired server framerate in frames per second,  backed by framePeriod
        {
            get { return 1000.0f / framePeriod; }
            set { framePeriod = (int)(1000.0f / value); }
        }

        public SS3DServer()
        {
            runlevel = RunLevel.Init;
            singleton = this;

            ConfigManager.Singleton.Initialize("./config.xml");
            LogManager.Initialize(ConfigManager.Singleton.Configuration.LogPath, LogLevel.Information);
        }
        #endregion


        public void LoadSettings()
        {
            var cfgmgr = ConfigManager.Singleton;
            serverPort = cfgmgr.Configuration.Port;
            serverName = cfgmgr.Configuration.ServerName;
            framePeriod = cfgmgr.Configuration.framePeriod;
            serverMapName = cfgmgr.Configuration.serverMapName;
            serverMaxPlayers = cfgmgr.Configuration.serverMaxPlayers;
            gameType = cfgmgr.Configuration.gameType;
            serverWelcomeMessage = cfgmgr.Configuration.serverWelcomeMessage;
            LogManager.Log("Port: " + serverPort.ToString());
            LogManager.Log("Name: " + serverName);
            LogManager.Log("Rate: " + (int)serverRate + " (" + framePeriod + " ms)");
            LogManager.Log("Map: " + serverMapName);
            LogManager.Log("Max players: " + serverMaxPlayers);
            LogManager.Log("Game type: " + gameType);
            LogManager.Log("Welcome message: " + serverWelcomeMessage);
        }

        /// <summary>
        /// Controls what modules are running.
        /// </summary>
        /// <param name="_runlevel"></param>
        public void InitModules(RunLevel _runlevel = RunLevel.Lobby)
        {
            if (_runlevel == runlevel)
                return;

            runlevel = _runlevel;
            if (runlevel == RunLevel.Lobby)
            {
                StartAt = DateTime.Now.AddSeconds(game_countdown);
            }
            else if (runlevel == RunLevel.Game)
            {
                map = new Map();
                map.InitMap(serverMapName);

                entityManager = new EntityManager(SS3DNetServer.Singleton);
                atomManager = new AtomManager(entityManager);
                //playerManager = new PlayerManager();
                atomManager.LoadAtoms();

                RoundManager.Singleton.CurrentGameMode.StartGame();
            }
        }

        public bool Start()
        {
            //try
            //{
                time = DateTime.Now;
                //LoadDataFile(dataFilename);
                LoadSettings();

                if (JobHandler.Singleton.LoadDefinitionsFromFile("JobDefinitions.xml"))
                {
                    LogManager.Log("Job Definitions File not found.", LogLevel.Fatal);
                    Environment.Exit(1);
                }
                else LogManager.Log("Job Definitions Found. " + JobHandler.Singleton.JobDefinitions.Count.ToString() + " Jobs loaded.", LogLevel.Information);

                netConfig.Port = serverPort;
                var netServer = new SS3DNetServer(netConfig);
                SS3DNetServer.Singleton.Start();

                chatManager = new ChatManager();
                playerManager = new PlayerManager();

                StartLobby();
                StartGame();
                
                active = true;
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
            InitModules(RunLevel.Lobby);
        }

        public void DisposeForRestart()
        {
            map = null; //Implement proper disposal.
            atomManager = null;
            GC.Collect();
        }

        public void Restart()
        {
            LogManager.Log("Restarting Server...");
            foreach (PlayerSession curr in playerManager.playerSessions.Values)
                curr.JoinLobby();
            DisposeForRestart();
            StartLobby();
        }

        public void StartGame()
        {
            InitModules(RunLevel.Game);
            playerManager.SendJoinGameToAll();
        }

        #region server mainloop
        
        // The main server loop
        public void MainLoop()
        {
            TimeSpan sleepTime;

            while (Active)
            {
                try
                {
                    FrameStart();
                    ProcessPackets();
                    Update(framePeriod);
                    sleepTime = time.AddMilliseconds(framePeriod) - DateTime.Now;

                    if (sleepTime.TotalMilliseconds > 0)
                        Thread.Sleep(sleepTime);
                    //else
                    //    Console.WriteLine("Server slow by " + sleepTime.TotalMilliseconds);
                }
                catch (Exception e)
                {
                    LogManager.Log(e.ToString(), LogLevel.Error);
                    active = false;
                }
            }
        }
        

        // called at the start of each server frame
        public void FrameStart()
        {
            time = DateTime.Now;
        }

        public void ProcessPackets()
        {
            try
            {
                NetIncomingMessage msg;
                while ((msg = SS3DNetServer.Singleton.ReadMessage()) != null)
                {
                    Console.Title = SS3DNetServer.Singleton.Statistics.SentBytes.ToString() + " " + SS3DNetServer.Singleton.Statistics.ReceivedBytes;
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
                            if (clientList.ContainsKey(msg.SenderConnection))
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
                    SS3DNetServer.Singleton.Recycle(msg);
                }                
            }
            catch
            {
            }
        }

        public void Update(float framePeriod)
        {
            if (runlevel == RunLevel.Game)
            {
                TimeSpan lastFrame = time - lastUpdate;
                if (lastFrame.TotalMilliseconds > framePeriod)
                {
                    atomManager.Update(framePeriod);
                    map.UpdateAtmos();
                    RoundManager.Singleton.CurrentGameMode.Update();
                }
            }
            else if (runlevel == RunLevel.Lobby)
            {
                TimeSpan countdown = StartAt.Subtract(DateTime.Now);
                if (lastAnnounced != countdown.Seconds)
                {
                    lastAnnounced = countdown.Seconds;
                    chatManager.SendChatMessage(ChatChannel.Server, "Starting in " + lastAnnounced.ToString() + " seconds...", "", 0);
                }
                if (countdown.Seconds <= 0)
                {
                    StartGame();
                }
            }
            lastUpdate = time;
        }
        #endregion

        public bool Active
        {
            get { return active; }
        }

        public void ShutDown()
        {

        }
        
        public void HandleConnectionApproval(NetConnection sender)
        {
            clientList.Add(sender, new Client(sender));
        }

        public void SendWelcomeInfo(NetConnection connection)
        {
            NetOutgoingMessage welcomeMessage = SS3DNetServer.Singleton.CreateMessage();
            welcomeMessage.Write((byte)NetMessage.WelcomeMessage);
            welcomeMessage.Write(serverName);
            welcomeMessage.Write(serverPort);
            welcomeMessage.Write(serverWelcomeMessage);
            welcomeMessage.Write(serverMaxPlayers);
            welcomeMessage.Write(serverMapName);
            welcomeMessage.Write(RoundManager.Singleton.CurrentGameMode.Name);
            SS3DNetServer.Singleton.SendMessage(welcomeMessage, connection, NetDeliveryMethod.ReliableOrdered);
            SendNewPlayerCount();
        }

        public void SendNewPlayerCount()
        {
            NetOutgoingMessage playercountMessage = SS3DNetServer.Singleton.CreateMessage();
            playercountMessage.Write((byte)NetMessage.PlayerCount);
            playercountMessage.Write((byte)clientList.Count);
            foreach (NetConnection conn in clientList.Keys) //Why is this sent to everyone?
            {
                SS3DNetServer.Singleton.SendMessage(playercountMessage, conn, NetDeliveryMethod.ReliableOrdered);
            }
        }

        public void SendPlayerList(NetConnection connection)
        {
            NetOutgoingMessage playerListMessage = SS3DNetServer.Singleton.CreateMessage();
            playerListMessage.Write((byte)NetMessage.PlayerList);
            playerListMessage.Write((byte)clientList.Count);

            foreach (NetConnection conn in clientList.Keys)
            {
                PlayerSession plrSession = playerManager.GetSessionByConnection(conn);
                playerListMessage.Write(plrSession.name);
                playerListMessage.Write((byte)plrSession.status);
                playerListMessage.Write(clientList[conn].netConnection.AverageRoundtripTime);
            }
            SS3DNetServer.Singleton.SendMessage(playerListMessage, connection, NetDeliveryMethod.ReliableOrdered);
        }

        public void HandleStatusChanged(NetIncomingMessage msg)
        {
            NetConnection sender = msg.SenderConnection;
            string senderIP = sender.RemoteEndpoint.Address.ToString();
            LogManager.Log(senderIP + ": Status change");

            if (sender.Status == NetConnectionStatus.Connected)
            {
                LogManager.Log(senderIP + ": Connection request");
                if (clientList.ContainsKey(sender))
                {
                    LogManager.Log(senderIP + ": Already connected", LogLevel.Error);
                    return;
                }
                else
                {
                    HandleConnectionApproval(sender);
                    playerManager.NewSession(sender); // TODO move this to somewhere that makes more sense.
                }
                // Send map
            }
            else if (sender.Status == NetConnectionStatus.Disconnected)
            {
                LogManager.Log(senderIP + " : Disconnected");

                playerManager.EndSession(sender);

                if (clientList.ContainsKey(sender))
                {
                    clientList.Remove(sender);
                }

            }
        }

        /// <summary>
        /// Main method for routing incoming application network messages
        /// </summary>
        /// <param name="msg"></param>
        public void HandleData(NetIncomingMessage msg)
        {
            NetMessage messageType = (NetMessage)msg.ReadByte();
            switch (messageType)
            {
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
                    chatManager.HandleNetMessage(msg);
                    break;
                case NetMessage.AtomManagerMessage:
                    atomManager.HandleNetworkMessage(msg);
                    break;
                case NetMessage.PlayerSessionMessage:
                    playerManager.HandleNetworkMessage(msg);
                    break;
                case NetMessage.MapMessage:
                    map.HandleNetworkMessage(msg);
                    break;
                case NetMessage.JobList:
                    HandleJobListRequest(msg);
                    break;
                case NetMessage.PlacementManagerMessage:
                    PlacementManager.Singleton.HandleNetMessage(msg);
                    break;
                case NetMessage.EntityMessage:
                    entityManager.HandleEntityNetworkMessage(msg);
                    break;
                case NetMessage.EntityManagerMessage:
                    entityManager.HandleNetworkMessage(msg);
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

                default:
                    break;
            }
        
        }

        public void HandleAdminMessage(NetMessage adminMsgType, NetIncomingMessage messageBody)
        {
            switch (adminMsgType)
            {
                case NetMessage.RequestAdminLogin:
                    string password = messageBody.ReadString();
                    if (password == ConfigManager.Singleton.Configuration.AdminPassword)
                    {
                        LogManager.Log("Admin login: " + messageBody.SenderConnection.RemoteEndpoint.Address.ToString());
                        playerManager.GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin = true;
                    }
                    else
                        LogManager.Log("Failed Admin login: " + messageBody.SenderConnection.RemoteEndpoint.Address.ToString() + " -> ' " + password + " '");
                    break;
                case NetMessage.RequestAdminPlayerlist:
                    if (playerManager.GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin == true)
                    {
                        NetOutgoingMessage AdminPlayerListMessage = SS3DNetServer.Singleton.CreateMessage();
                        AdminPlayerListMessage.Write((byte)NetMessage.RequestAdminPlayerlist);
                        AdminPlayerListMessage.Write((byte)clientList.Count);
                        foreach (NetConnection conn in clientList.Keys)
                        {
                            PlayerSession plrSession = playerManager.GetSessionByConnection(conn);
                            AdminPlayerListMessage.Write(plrSession.name);
                            AdminPlayerListMessage.Write((byte)plrSession.status);
                            AdminPlayerListMessage.Write(plrSession.assignedJob.Name);
                            AdminPlayerListMessage.Write(plrSession.connectedClient.RemoteEndpoint.Address.ToString());
                        }
                        SS3DNetServer.Singleton.SendMessage(AdminPlayerListMessage, messageBody.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    }
                    else
                    {
                        NetOutgoingMessage LoginMessage = SS3DNetServer.Singleton.CreateMessage();
                        LoginMessage.Write((byte)NetMessage.RequestAdminLogin);
                        SS3DNetServer.Singleton.SendMessage(LoginMessage, messageBody.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    }
                    break;
                case NetMessage.RequestAdminKick:
                    if (playerManager.GetSessionByConnection(messageBody.SenderConnection).adminPermissions.isAdmin == true)
                    {
                        PlayerSession kickSession = playerManager.GetSessionByIp(messageBody.ReadString());
                        if (kickSession != null)
                        {
                            playerManager.EndSession(kickSession.connectedClient);
                            kickSession.connectedClient.Deny("Kicked by Administrator.");
                        }
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
            if (pickedJob != null)
            {
                PlayerSession session =  playerManager.GetSessionByConnection(msg.SenderConnection);
                session.assignedJob = pickedJob;

                NetOutgoingMessage JobSelectedMessage = SS3DNetServer.Singleton.CreateMessage();
                JobSelectedMessage.Write((byte)NetMessage.JobSelected);
                JobSelectedMessage.Write(pickedJob.Name);
                SS3DNetServer.Singleton.SendMessage(JobSelectedMessage, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
            }
        }

        public void HandleJobListRequest(NetIncomingMessage msg)
        {
            PlayerSession p = playerManager.GetSessionByConnection(msg.SenderConnection);
            NetOutgoingMessage JobListMessage = SS3DNetServer.Singleton.CreateMessage();
            JobListMessage.Write((byte)NetMessage.JobList);
            JobListMessage.Write(JobHandler.Singleton.GetDefinitionsString());
            SS3DNetServer.Singleton.SendMessage(JobListMessage, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered); //OFF WE GO. WHEEEE.
        }

        public void HandleClientName(NetIncomingMessage msg)
        {
            string name = msg.ReadString();
            clientList[msg.SenderConnection].SetName(name);
            string fixedname = name.Trim();
            if (fixedname.Length < 3)
                fixedname = "Player";
            PlayerSession p = playerManager.GetSessionByConnection(msg.SenderConnection);
            p.SetName(fixedname);
       }

        // The size of the map being sent is almost exaclty 1 byte per tile.
        // The default 30x30 map is 900 bytes, a 100x100 one is 10,000 bytes (10kb).
        public void SendMap(NetConnection connection)
        {
            LogManager.Log(connection.RemoteEndpoint.Address.ToString() + ": Sending map");
            NetOutgoingMessage mapMessage = SS3DNetServer.Singleton.CreateMessage();
            mapMessage.Write((byte)NetMessage.SendMap);

            //TileType[,] mapObjectTypes = map.GetMapForSending();
            int mapWidth = map.GetMapWidth();
            int mapHeight = map.GetMapHeight();

            mapMessage.Write(mapWidth);
            mapMessage.Write(mapHeight);

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Tiles.Tile t = map.GetTileAt(x, y);
                    mapMessage.Write((byte)t.tileType);
                    mapMessage.Write((byte)t.tileState);
                }
            }

            SS3DNetServer.Singleton.SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
            LogManager.Log(connection.RemoteEndpoint.Address.ToString() + ": Sending map finished with message size: " + mapMessage.LengthBytes + " bytes");

            // Lets also send them all the items and mobs.
            atomManager.NewPlayer(connection);
            //playerManager.SpawnPlayerMob(playerManager.GetSessionByConnection(connection));
            //Send atmos state to player
            map.SendAtmosStateTo(connection);
            //Todo: Preempt this with the lobby.
            RoundManager.Singleton.SpawnPlayer(playerManager.GetSessionByConnection(connection)); //SPAWN
        }

        public void SendChangeTile(int x, int z, TileType newType)
        {
            NetOutgoingMessage tileMessage = SS3DNetServer.Singleton.CreateMessage();
            //tileMessage.Write((byte)NetMessage.ChangeTile);
            tileMessage.Write(x);
            tileMessage.Write(z);
            tileMessage.Write((byte)newType);
            foreach(NetConnection connection in clientList.Keys)
            {
                SS3DNetServer.Singleton.SendMessage(tileMessage, connection, NetDeliveryMethod.ReliableOrdered);
                LogManager.Log(connection.RemoteEndpoint.Address.ToString() + ": Tile Change Being Sent", LogLevel.Debug);
            }
        }

        public void SendMessageToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            if (message == null)
            {
                return;
            }
            int i = clientList.Count;
            //Console.WriteLine("Sending to all ("+i+") with size: " + message.LengthBits + " bytes");

            SS3DNetServer.Singleton.SendMessage(message, SS3DNetServer.Singleton.Connections, method, 0);
        }

        public void SendMessageTo(NetOutgoingMessage message, NetConnection connection, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            if (message == null || connection == null)
            {
                return;
            }
            LogManager.Log("Sending to one with size: " + message.LengthBytes + " bytes", LogLevel.Debug);
            SS3DNetServer.Singleton.SendMessage(message, connection, method);
        }
    }
}
