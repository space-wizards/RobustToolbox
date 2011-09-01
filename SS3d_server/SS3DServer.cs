using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using SS3D_Server.Modules;
using SS3D_Server.Modules.Client;
using SS3D_Server.Modules.Map;
//using SS3d_server.Modules.Items;
//using SS3d_server.Modules.Mobs;
using SS3D_Server.Modules.Chat;
using SS3D_Server.Atom;

using Lidgren.Network;
using SS3D_shared;

namespace SS3D_Server
{
    public class SS3DServer
    {
        private NetPeerConfiguration netConfig = new NetPeerConfiguration("SS3D_NetTag");
        public Dictionary<NetConnection, Client> clientList = new Dictionary<NetConnection, Client>();
        public Map map;
        //public ItemManager itemManager;
        //public MobManager mobManager;
        public ChatManager chatManager;
        public AtomManager atomManager;
        public PlayerManager playerManager;
        public RunLevel runlevel {get;private set;}

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
            LogManager.Initialize(ConfigManager.Singleton.Configuration.LogPath);
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
                chatManager = new ChatManager();
                playerManager = new PlayerManager();
            }
            else if (runlevel == RunLevel.Game)
            {
                map = new Map();
                map.InitMap(serverMapName);

                atomManager = new AtomManager();
                playerManager = new PlayerManager();
            }
            
        }

        public bool Start()
        {
            try
            {
                time = DateTime.Now;
                //LoadDataFile(dataFilename);
                LoadSettings();
                netConfig.Port = serverPort;
                var netServer = new SS3DNetServer(netConfig);
                SS3DNetServer.Singleton.Start();

                StartLobby();
                StartGame();
                
                active = true;
                return false;
            }
            catch (Lidgren.Network.NetException e)
            {
                LogManager.Log(e.Message, LogLevel.Error);
                active = false;
                return true;
            }
            catch (Exception e)
            {
                LogManager.Log(e.Message, LogLevel.Error);
                active = false;
                return true;
            }
        }

        public void StartLobby()
        {
            InitModules(RunLevel.Lobby);
        }

        public void StartGame()
        {
            InitModules(RunLevel.Game);
            AddRandomCrowbars();
            playerManager.SendJoinGameToAll();
        }

        #region server mainloop
        
        // The main server loop
        public void MainLoop()
        {
            TimeSpan sleepTime;

            while (Active)
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
            welcomeMessage.Write((byte)gameType);
            SS3DNetServer.Singleton.SendMessage(welcomeMessage, connection, NetDeliveryMethod.ReliableOrdered);
            SendNewPlayerCount();
        }

        public void SendNewPlayerCount()
        {
            NetOutgoingMessage playercountMessage = SS3DNetServer.Singleton.CreateMessage();
            playercountMessage.Write((byte)NetMessage.PlayerCount);
            playercountMessage.Write((byte)clientList.Count);
            foreach (NetConnection conn in clientList.Keys)
            {
                SS3DNetServer.Singleton.SendMessage(playercountMessage, conn, NetDeliveryMethod.ReliableOrdered);
            }
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
                LogManager.Log(senderIP + ": Disconnected");

                playerManager.EndSession(sender);

                if (clientList.ContainsKey(sender))
                {
                    clientList.Remove(sender);
                }

            }
        }

        public void HandleData(NetIncomingMessage msg)
        {
            NetMessage messageType = (NetMessage)msg.ReadByte();
            switch (messageType)
            {
                case NetMessage.WelcomeMessage:
                    SendWelcomeInfo(msg.SenderConnection);
                    break;
                case NetMessage.SendMap:
                    SendMap(msg.SenderConnection);
                    break;
                case NetMessage.LobbyChat:
                    HandleLobbyChat(msg);
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
                default:
                    break;
            }
        
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

        [Obsolete]
        public void HandleLobbyChat(NetIncomingMessage msg)
        {
            string text = clientList[msg.SenderConnection].playerName + ": ";
            text += msg.ReadString();
            SendLobbyChat(text);
        }

        [Obsolete]
        public void SendLobbyChat(string text)
        {
            NetOutgoingMessage chatMessage = SS3DNetServer.Singleton.CreateMessage();
            chatMessage.Write((byte)NetMessage.LobbyChat);
            chatMessage.Write(text);
            foreach (NetConnection connection in clientList.Keys)
            {
                SS3DNetServer.Singleton.SendMessage(chatMessage, connection, NetDeliveryMethod.ReliableOrdered);
            }
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
            playerManager.SpawnPlayerMob(playerManager.GetSessionByConnection(connection));
            //Send atmos state to player
            map.SendAtmosStateTo(connection);
            //Todo: Preempt this with the lobby.
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
            foreach (Client client in clientList.Values)
            {
                SS3DNetServer.Singleton.SendMessage(message, client.netConnection, method);
            }
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

        public void AddRandomCrowbars()
        {
            // this is just getting stupid now
            /*Atom.Item.Tool.Crowbar c;
            Atom.Item.Tool.Welder g;
            Atom.Item.Tool.Wrench e;
            Atom.Item.Container.Toolbox t;
            Atom.Object.Door.Door d;
            Atom.Item.Misc.Flashlight f;

            Random r = new Random();
            for (int i = 0; i < 10; i++)
            {
                c = (Atom.Item.Tool.Crowbar)atomManager.SpawnAtom("Atom.Item.Tool.Crowbar");
                c.Translate(new SS3D_shared.HelperClasses.Vector2(r.NextDouble() * map.GetMapWidth() * map.tileSpacing, r.NextDouble() * map.GetMapHeight() * map.tileSpacing));
            }
            for (int i = 0; i < 10; i++)
            {
                g = (Atom.Item.Tool.Welder)atomManager.SpawnAtom("Atom.Item.Tool.Welder");
                g.Translate(new SS3D_shared.HelperClasses.Vector2(r.NextDouble() * map.GetMapWidth() * map.tileSpacing, r.NextDouble() * map.GetMapHeight() * map.tileSpacing));
            }
            for (int i = 0; i < 10; i++)
            {
                e = (Atom.Item.Tool.Wrench)atomManager.SpawnAtom("Atom.Item.Tool.Wrench");
                e.Translate(new SS3D_shared.HelperClasses.Vector2(r.NextDouble() * map.GetMapWidth() * map.tileSpacing, r.NextDouble() * map.GetMapHeight() * map.tileSpacing));
            }
            for (int i = 0; i < 1; i++)
            {
                t = (Atom.Item.Container.Toolbox)atomManager.SpawnAtom("Atom.Item.Container.Toolbox");
                t.Translate(new SS3D_shared.HelperClasses.Vector2(r.NextDouble() * map.GetMapWidth() * map.tileSpacing, r.NextDouble() * map.GetMapHeight() * map.tileSpacing));
            }

            for (int i = 0; i < 10; i++)
            {
                f = (Atom.Item.Misc.Flashlight)atomManager.SpawnAtom("Atom.Item.Misc.Flashlight");
                f.Translate(new SS3D_shared.HelperClasses.Vector2(r.NextDouble() * map.GetMapWidth() * map.tileSpacing, r.NextDouble() * map.GetMapHeight() * map.tileSpacing));
            }

            f = (Atom.Item.Misc.Flashlight)atomManager.SpawnAtom("Atom.Item.Misc.Flashlight");
            f.Translate(new SS3D_shared.HelperClasses.Vector2(2 * map.tileSpacing, 2 * map.tileSpacing));
            f.light.color.b = 0;
            f.light.color.g = 0;
            f.light.color.r = 254;

            d = (Atom.Object.Door.Door)atomManager.SpawnAtom("Atom.Object.Door.Door");
            d.Translate(new SS3D_shared.HelperClasses.Vector2(304, 336));

            d = (Atom.Object.Door.Door)atomManager.SpawnAtom("Atom.Object.Door.Door");
            d.Translate(new SS3D_shared.HelperClasses.Vector2(304, 432));

            d = (Atom.Object.Door.Door)atomManager.SpawnAtom("Atom.Object.Door.Door");
            d.Translate(new SS3D_shared.HelperClasses.Vector2(592, 336));

            d = (Atom.Object.Door.Door)atomManager.SpawnAtom("Atom.Object.Door.Door");
            d.Translate(new SS3D_shared.HelperClasses.Vector2(592, 432));*/
            atomManager.LoadAtoms();
        }
    }
}
