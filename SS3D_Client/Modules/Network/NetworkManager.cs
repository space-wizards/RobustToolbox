using System;
using System.Text;

using SS3D.Modules;
using SS3D.States;
using System.Collections.Generic;

using SS3D_shared;

using Lidgren.Network;

namespace SS3D.Modules.Network
{
    public delegate void NetworkMsgHandler(NetworkManager netMgr, NetIncomingMessage msg);
    public delegate void NetworkStateHandler(NetworkManager netMgr);

    public class NetworkManager
    {
        private Program prg;
        private StateManager mStateMgr;
        private TileType[,] tileArray;
        private Map.Map mMap;
        private GameType serverGameType;
        public bool mapRecieved = false;
        private int mapWidth;
        private int mapHeight;
        private string serverName = "SS3D Server";

        public bool isConnected 
        { 
            get; 
            private set; 
        }

        public NetClient netClient
        {
            get;
            private set;
        }

        private NetPeerConfiguration netConfig = new NetPeerConfiguration("SS3D_NetTag");

        public event NetworkMsgHandler MessageArrived;  //Called when we recieve a new message.
        protected virtual void OnMessageArrived(NetIncomingMessage msg)
        {
            if (MessageArrived != null) MessageArrived(this, msg);
        }

        public event NetworkStateHandler Connected;     //Called when we connect to a server.
        protected virtual void OnConnected()
        {
            if (Connected != null) Connected(this);
        }

        public event NetworkStateHandler Disconnected;  //Called when we Disconnect from a server.
        protected virtual void OnDisconnected()
        {
            if (Disconnected != null) Disconnected(this);
        }

        public NetworkManager(Program _prg)
        {
            prg = _prg;
            mStateMgr = prg.mStateMgr;

            isConnected = false;

            netClient = new NetClient(netConfig);
            netClient.Start();
        }

        public void SetMap(Map.Map _map)
        {
            mMap = _map;
        }

        public void ConnectTo(string host)
        {
          netClient.Connect(host,1212);
        }

        public void Disconnect()
        {
            Restart();
        }

        public void Restart()
        {
            netClient.Shutdown("Leaving");
            netClient = new NetClient(netConfig);
            netClient.Start();
        }

        public void UpdateNetwork()
        {
            if (!isConnected && netClient.ServerConnection != null)
            {
                OnConnected();
                isConnected = true;
            }
            else if (isConnected && netClient.ServerConnection == null)
            {
                OnDisconnected();
                isConnected = false;
            }

            if (isConnected)
            {
                NetIncomingMessage msg;
                while ((msg = netClient.ReadMessage()) != null)
                {
                    OnMessageArrived(msg);
                    netClient.Recycle(msg);
                }
            }
        }

        public void ShutDown()
        {
            netClient.Shutdown("Quitting");
        }

        public void SetGameType(NetIncomingMessage msg)
        {
            serverGameType = (GameType)msg.ReadByte();
        }

        public void RequestMap()
        {
            NetOutgoingMessage message = netClient.CreateMessage();
            message.Write((byte)NetMessage.SendMap);
            netClient.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public void SendChangeTile(int x, int z, TileType newTile)
        {
            NetOutgoingMessage netMessage = netClient.CreateMessage();
            //netMessage.Write((byte)NetMessage.ChangeTile);
            netMessage.Write(x);
            netMessage.Write(z);
            netMessage.Write((byte)newTile);
            netClient.SendMessage(netMessage, NetDeliveryMethod.ReliableOrdered);
        }

        public NetOutgoingMessage GetMessage()
        {
            return netClient.CreateMessage();
        }

        public void SendClientName(string name)
        {
            NetOutgoingMessage message = netClient.CreateMessage();
            message.Write((byte)NetMessage.ClientName);
            message.Write(name);
            netClient.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendMessage(NetOutgoingMessage message, NetDeliveryMethod deliveryMethod)
        {
            if (message != null)
            {
                netClient.SendMessage(message, deliveryMethod);
            }
        }

        public NetIncomingMessage GetNetworkUpdate()
        {
            NetIncomingMessage msg;
            if((msg = netClient.ReadMessage()) != null)
            {
                return msg;
            }

            return null;
        }

        public TileType[,] GetTileArray()
        {
            return tileArray;
        }

        public int GetMapHeight()
        {
            return mapHeight;
        }

        public int GetMapWidth()
        {
            return mapWidth;
        }

        public string GetServerName()
        {
            return serverName;
        }

        public string GetServerAddress()
        {
            return (netClient.ServerConnection.RemoteEndpoint.Address.ToString() + ":" + netClient.Port.ToString());
        }

        public GameType GetGameType()
        {
            return serverGameType;
        }
    }
}
