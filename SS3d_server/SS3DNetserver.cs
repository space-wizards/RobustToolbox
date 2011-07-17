using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using SS3d_server.Modules;
using SS3d_server.Modules.Client;
using SS3d_server.Modules.Map;
//using SS3d_server.Modules.Items;
//using SS3d_server.Modules.Mobs;
using SS3d_server.Modules.Chat;
using SS3d_server.Atom;

using Lidgren.Network;
using SS3D_shared;

namespace SS3d_server
{
    public class SS3DNetserver
    {
        public NetServer netServer;
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

        public DateTime time;   // The server current frame time
        
        #region Server Settings
        string dataFilename = "ServerSettings.cfg";
        Dictionary<string, string> serverSettings = new Dictionary<string, string>();
        int serverPort = 1212;
        string serverName = "SS3D Server";
        string serverMapName = "SavedMap";
        string serverWelcomeMessage = "Welcome to the server!";
        int serverMaxPlayers = 32;
        GameType gameType = GameType.Game;
        
        public int framePeriod = 33; // The time (in milliseconds) between server frames

        public float serverRate     // desired server framerate in frames per second,  backed by framePeriod
        {
            get { return 1000.0f / framePeriod; }
            set { framePeriod = (int)(1000.0f / value); }
        }

        public SS3DNetserver()
        {
            runlevel = RunLevel.Init;
        }
        #endregion

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
                chatManager = new ChatManager(this);
                playerManager = new PlayerManager(this);
            }
            else if (runlevel == RunLevel.Game)
            {
                map = new Map();
                map.InitMap(serverMapName);

                atomManager = new AtomManager(this);
                playerManager = new PlayerManager(this);
            }
            
        }

        public bool Start()
        {
            try
            {
                time = DateTime.Now;
                LoadDataFile(dataFilename);
                LoadSettings();
                netConfig.Port = serverPort;
                netServer = new NetServer(netConfig);
                netServer.Start();

                StartLobby();
                StartGame();
                
                active = true;
                return false;
            }
            catch (Lidgren.Network.NetException e)
            {
                FileStream fs = new FileStream("Server Errors.txt", FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine("----------" + DateTime.Now.ToString() + "----------");
                sw.Write(e.Message);
                sw.WriteLine();
                sw.WriteLine();
                sw.Close();
                fs.Close();
                Console.WriteLine(e.Message);
                active = false;
                return true;
            }
            catch (Exception e)
            {
                FileStream fs = new FileStream("Server Errors.txt", FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine("----------" + DateTime.Now.ToString() + "----------");
                sw.Write(e.Message);
                sw.WriteLine();
                sw.WriteLine();
                sw.Close();
                fs.Close();
                Console.WriteLine(e.Message);
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
                while ((msg = netServer.ReadMessage()) != null)
                {
                    Console.Title = netServer.Statistics.SentBytes.ToString() + " " + netServer.Statistics.ReceivedBytes;
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                            Console.WriteLine(msg.ReadString());
                            break;

                        case NetIncomingMessageType.DebugMessage:
                            Console.WriteLine(msg.ReadString());
                            break;

                        case NetIncomingMessageType.WarningMessage:
                            Console.WriteLine(msg.ReadString());
                            break;

                        case NetIncomingMessageType.ErrorMessage:
                            Console.WriteLine(msg.ReadString());
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
                            Console.WriteLine("Unhandled type: " + msg.MessageType);
                            break;
                    }
                    netServer.Recycle(msg);
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
                atomManager.Update(framePeriod);
            }
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
            NetOutgoingMessage welcomeMessage = netServer.CreateMessage();
            welcomeMessage.Write((byte)NetMessage.WelcomeMessage);
            welcomeMessage.Write(serverName);
            welcomeMessage.Write(serverPort);
            welcomeMessage.Write(serverWelcomeMessage);
            welcomeMessage.Write(serverMaxPlayers);
            welcomeMessage.Write(serverMapName);
            welcomeMessage.Write((byte)gameType);
            netServer.SendMessage(welcomeMessage, connection, NetDeliveryMethod.ReliableOrdered);
            SendNewPlayerCount();
        }

        public void SendNewPlayerCount()
        {
            NetOutgoingMessage playercountMessage = netServer.CreateMessage();
            playercountMessage.Write((byte)NetMessage.PlayerCount);
            playercountMessage.Write((byte)clientList.Count);
            foreach (NetConnection conn in clientList.Keys)
            {
                netServer.SendMessage(playercountMessage, conn, NetDeliveryMethod.ReliableOrdered);
            }
        }

        public void HandleStatusChanged(NetIncomingMessage msg)
        {
            NetConnection sender = msg.SenderConnection;
            string senderIP = sender.RemoteEndpoint.Address.ToString();
            Console.WriteLine(senderIP + ": Status change");

            if (sender.Status == NetConnectionStatus.Connected)
            {
                Console.WriteLine(senderIP + ": Connection request");
                if (clientList.ContainsKey(sender))
                {
                    Console.WriteLine(senderIP + ": Already connected");
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
                Console.WriteLine(senderIP + ": Disconnected");

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
                case NetMessage.ChangeTile:
                    HandleChangeTile(msg);
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
            NetOutgoingMessage chatMessage = netServer.CreateMessage();
            chatMessage.Write((byte)NetMessage.LobbyChat);
            chatMessage.Write(text);
            foreach (NetConnection connection in clientList.Keys)
            {
                netServer.SendMessage(chatMessage, connection, NetDeliveryMethod.ReliableOrdered);
            }
        }

        // The size of the map being sent is almost exaclty 1 byte per tile.
        // The default 30x30 map is 900 bytes, a 100x100 one is 10,000 bytes (10kb).
        public void SendMap(NetConnection connection)
        {
            Console.WriteLine(connection.RemoteEndpoint.Address.ToString() + ": Sending map");
            NetOutgoingMessage mapMessage = netServer.CreateMessage();
            mapMessage.Write((byte)NetMessage.SendMap);

            TileType[,] mapObjectTypes = map.GetMapForSending();
            int mapWidth = map.GetMapWidth();
            int mapHeight = map.GetMapHeight();

            mapMessage.Write(mapWidth);
            mapMessage.Write(mapHeight);

            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    mapMessage.Write((byte)mapObjectTypes[x, z]);
                }
            }

            netServer.SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
            Console.WriteLine(connection.RemoteEndpoint.Address.ToString() + ": Sending map finished with message size: " + mapMessage.LengthBytes + " bytes");

            // Lets also send them all the items and mobs.
            atomManager.NewPlayer(connection);
            playerManager.SpawnPlayerMob(playerManager.GetSessionByConnection(connection));
            //Todo: Preempt this with the lobby.
        }

        public void HandleChangeTile(NetIncomingMessage msg)
        {
            Console.WriteLine(msg.SenderConnection.RemoteEndpoint.Address.ToString() + ": Tile Change Recieved");
 
            int x = msg.ReadInt32();
            int z = msg.ReadInt32();
            TileType newType = (TileType)msg.ReadByte();
            if (map.ChangeTile(x, z, newType))
            {
                SendChangeTile(x, z, newType);
            }
        }

        public void SendChangeTile(int x, int z, TileType newType)
        {
            NetOutgoingMessage tileMessage = netServer.CreateMessage();
            tileMessage.Write((byte)NetMessage.ChangeTile);
            tileMessage.Write(x);
            tileMessage.Write(z);
            tileMessage.Write((byte)newType);
            foreach(NetConnection connection in clientList.Keys)
            {
                netServer.SendMessage(tileMessage, connection, NetDeliveryMethod.ReliableOrdered);
                Console.WriteLine(connection.RemoteEndpoint.Address.ToString() + ": Tile Change Being Sent");
            }
        }

        private void LoadDataFile(string filename)
        {
            serverSettings = new Dictionary<string, string>();

            if (!File.Exists(filename))
            {
                WriteNewDataFile();
            }

            FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(file);

            string line = sr.ReadLine();
            while (line != null)
            {
                string[] args = line.Split("=".ToCharArray());
                if (args.Length == 2 && line[0] != '#')
                {
                    serverSettings[args[0].Trim()] = args[1].Trim();
                }
                line = sr.ReadLine();
            }

            sr.Close();
            file.Close();

        }

        public void LoadSettings()
        {
            if(serverSettings.ContainsKey("Port"))
            {
                serverPort = int.Parse(serverSettings["Port"]);
            }
            Console.WriteLine("Port: " + serverPort);
            if (serverSettings.ContainsKey("Name"))
            {
                serverName = serverSettings["Name"];
            }
            Console.WriteLine("Name: " + serverName);
            if (serverSettings.ContainsKey("Rate"))
            {
                serverRate = float.Parse(serverSettings["Rate"]);
            }
            Console.WriteLine("Rate: " + (int)serverRate+ " ("+framePeriod+" ms)");
            if (serverSettings.ContainsKey("MapName"))
            {
                serverMapName = serverSettings["MapName"];
            }
            Console.WriteLine("Map: " + serverMapName);
            if (serverSettings.ContainsKey("MaxPlayers"))
            {
                serverMaxPlayers = int.Parse(serverSettings["MaxPlayers"]);
            }
            Console.WriteLine("Max players: " + serverMaxPlayers);
            if (serverSettings.ContainsKey("GameType"))
            {
                gameType = (GameType)byte.Parse(serverSettings["GameType"]);
            }
            Console.WriteLine("Game type: " + gameType);
            if (serverSettings.ContainsKey("WelcomeMessage"))
            {
                serverWelcomeMessage = serverSettings["WelcomeMessage"];
            }
            Console.WriteLine("Welcome message: " + serverWelcomeMessage);
        }

        public void WriteNewDataFile()
        {
            Console.WriteLine("ServerSettings.config not found. Generating default file.");
            FileStream fs = new FileStream(dataFilename, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            sw.WriteLine("#Server name");
            sw.WriteLine("Name=" + serverName);
            sw.WriteLine("#Server Port");
            sw.WriteLine("Port=" + serverPort);
            sw.WriteLine("#Welcome message to send to clients on connect");
            sw.WriteLine("WelcomeMessage=" + serverWelcomeMessage);
            sw.WriteLine("#Game Type: 0 = Map editor, 1 = Game");
            sw.WriteLine("GameType=" + (byte)gameType);
            sw.WriteLine("#The name of the file containing the map to load");
            sw.WriteLine("MapName=" + serverMapName);
            sw.WriteLine("#The max number of players allowed");
            sw.WriteLine("MaxPlayers=" + serverMaxPlayers);
            sw.WriteLine("#The target server update rate in frames per second");
            sw.WriteLine("Rate=" + serverRate);


            sw.Close();
            fs.Close();
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
                netServer.SendMessage(message, client.netConnection, method);
            }
        }

        public void SendMessageTo(NetOutgoingMessage message, NetConnection connection, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            if (message == null || connection == null)
            {
                return;
            }
            Console.WriteLine("Sending to one with size: " + message.LengthBytes + " bytes");
            netServer.SendMessage(message, connection, method);
        }

        public void AddRandomCrowbars()
        {
            Atom.Item.Tool.Crowbar c;
            Atom.Item.Container.Toolbox t;
            Atom.Object.Door.Door d;
            Atom.Item.Misc.Flashlight f;

            Random r = new Random();
            for (int i = 0; i < 10; i++)
            {
                c = (Atom.Item.Tool.Crowbar)atomManager.SpawnAtom("Atom.Item.Tool.Crowbar");
                c.Translate(new SS3D_shared.HelperClasses.Vector2(r.NextDouble() * map.GetMapWidth() * map.tileSpacing, r.NextDouble() * map.GetMapHeight() * map.tileSpacing));
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

            d = (Atom.Object.Door.Door)atomManager.SpawnAtom("Atom.Object.Door.Door");
            d.Translate(new SS3D_shared.HelperClasses.Vector2(304, 336));

            d = (Atom.Object.Door.Door)atomManager.SpawnAtom("Atom.Object.Door.Door");
            d.Translate(new SS3D_shared.HelperClasses.Vector2(304, 432));

            d = (Atom.Object.Door.Door)atomManager.SpawnAtom("Atom.Object.Door.Door");
            d.Translate(new SS3D_shared.HelperClasses.Vector2(592, 336));

            d = (Atom.Object.Door.Door)atomManager.SpawnAtom("Atom.Object.Door.Door");
            d.Translate(new SS3D_shared.HelperClasses.Vector2(592, 432));
        }
    }
}
