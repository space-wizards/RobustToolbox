using System;
using ClientInterfaces.Configuration;
using ClientInterfaces.Map;
using ClientInterfaces.Network;
using ClientServices.Map;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;

namespace ClientServices.Network
{
    public class NetworkManager : INetworkManager
    {
        private const string ServerName = "SS13 Server";
        private readonly NetPeerConfiguration _netConfig = new NetPeerConfiguration("SS13_NetTag");
        private NetClient _netClient;
        private GameType _serverGameType;

        public NetworkManager()
        {
            IsConnected = false;

            var config = IoCManager.Resolve<IConfigurationManager>();

            //Simulate Latency
            if (config.GetSimulateLatency())
            {
                _netConfig.SimulatedLoss = config.GetSimulatedLoss();
                _netConfig.SimulatedMinimumLatency = config.GetSimulatedMinimumLatency();
                _netConfig.SimulatedRandomLatency = config.GetSimulatedRandomLatency();
            }

            _netClient = new NetClient(_netConfig);
            _netClient.Start();
        }

        #region INetworkManager Members

        public bool IsConnected { get; private set; }

        public NetPeerStatistics CurrentStatistics
        {
            get { return _netClient.Statistics; }
        }

        public long UniqueId
        {
            get { return _netClient.UniqueIdentifier; }
        }

        public event EventHandler<IncomingNetworkMessageArgs> MessageArrived; //Called when we recieve a new message.

        public event EventHandler Connected; //Called when we connect to a server.

        public event EventHandler Disconnected; //Called when we Disconnect from a server.

        public void ConnectTo(string host)
        {
            _netClient.Connect(host, 1212);
        }

        public void Disconnect()
        {
            Restart();
        }

        public void UpdateNetwork()
        {
            if (IsConnected)
            {
                NetIncomingMessage msg;
                while ((msg = _netClient.ReadMessage()) != null)
                {
                    OnMessageArrived(msg);
                    _netClient.Recycle(msg);
                }
            }

            if (!IsConnected && _netClient.ServerConnection != null)
            {
                OnConnected();
                IsConnected = true;
            }
            else if (IsConnected && _netClient.ServerConnection == null)
            {
                OnDisconnected();
                IsConnected = false;
            }
        }

        public void RequestMap()
        {
            NetOutgoingMessage message = _netClient.CreateMessage();
            message.Write((byte) NetMessage.RequestMap);
            _netClient.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public NetOutgoingMessage CreateMessage()
        {
            return _netClient.CreateMessage();
        }

        public void SendClientName(string name)
        {
            NetOutgoingMessage message = _netClient.CreateMessage();
            message.Write((byte) NetMessage.ClientName);
            message.Write(name);
            _netClient.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendMessage(NetOutgoingMessage message, NetDeliveryMethod deliveryMethod)
        {
            if (message != null)
            {
                _netClient.SendMessage(message, deliveryMethod);
            }
        }

        #endregion

        protected virtual void OnMessageArrived(NetIncomingMessage message)
        {
            if (MessageArrived != null) MessageArrived(this, new IncomingNetworkMessageArgs(message));
        }

        protected virtual void OnConnected()
        {
            if (Connected != null) Connected(this, null);
        }

        protected virtual void OnDisconnected()
        {
            if (Disconnected != null) Disconnected(this, null);
        }

        public void Restart()
        {
            _netClient.Shutdown("Leaving");
            _netClient = new NetClient(_netConfig);
            _netClient.Start();
        }

        public void ShutDown()
        {
            _netClient.Shutdown("Quitting");
        }

        public void SetGameType(NetIncomingMessage msg)
        {
            _serverGameType = (GameType) msg.ReadByte();
        }

        public void SendChangeTile(int x, int z, string newTile)
        {
            var mapMgr = (MapManager) IoCManager.Resolve<IMapManager>();
            NetOutgoingMessage netMessage = _netClient.CreateMessage();
            netMessage.Write(x);
            netMessage.Write(z);
            netMessage.Write(mapMgr.GetTileIndex(newTile));
            _netClient.SendMessage(netMessage, NetDeliveryMethod.ReliableOrdered);
        }

        public NetIncomingMessage GetNetworkUpdate()
        {
            NetIncomingMessage msg;
            return (msg = _netClient.ReadMessage()) != null ? msg : null;
        }

        public string GetServerName()
        {
            return ServerName;
        }

        public string GetServerAddress()
        {
            return String.Format("{0}:{1}", _netClient.ServerConnection.RemoteEndPoint.Address, _netClient.Port);
        }

        public GameType GetGameType()
        {
            return _serverGameType;
        }
    }
}