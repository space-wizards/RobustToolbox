using Lidgren.Network;
using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Network;
using SS14.Client.Services.Map;
using SS14.Shared;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.Services.Network
{
    public class NetworkManager : INetworkManager
    {
        private const string ServerName = "SS13 Server";
        private readonly NetPeerConfiguration _netConfig = new NetPeerConfiguration("SS13_NetTag");
        public NetClient NetClient { get; private set; }
        private GameType _serverGameType;

        public NetworkManager()
        {
            IsConnected = false;

            var config = IoCManager.Resolve<IConfigurationManager>();

            //Simulate Latency
            if (config.GetSimulateLatency())
            {
#if DEBUG
                _netConfig.SimulatedLoss = config.GetSimulatedLoss();
                _netConfig.SimulatedMinimumLatency = config.GetSimulatedMinimumLatency();
                _netConfig.SimulatedRandomLatency = config.GetSimulatedRandomLatency();
#endif
            }

#if DEBUG
            _netConfig.ConnectionTimeout = 30000f;
#endif

            NetClient = new NetClient(_netConfig);
            NetClient.Start();
        }

        #region INetworkManager Members

        public bool IsConnected { get; private set; }

        public NetPeerStatistics CurrentStatistics
        {
            get { return NetClient.Statistics; }
        }

        public long UniqueId
        {
            get { return NetClient.UniqueIdentifier; }
        }

        public event EventHandler<IncomingNetworkMessageArgs> MessageArrived; //Called when we recieve a new message.

        public event EventHandler Connected; //Called when we connect to a server.

        public event EventHandler Disconnected; //Called when we Disconnect from a server.

        public void ConnectTo(string host)
        {
            NetClient.Connect(host, 1212);
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
                while ((msg = NetClient.ReadMessage()) != null)
                {
                    OnMessageArrived(msg);
                    NetClient.Recycle(msg);
                }
            }

            if (!IsConnected && NetClient.ServerConnection != null)
            {
                OnConnected();
                IsConnected = true;
            }
            else if (IsConnected && NetClient.ServerConnection == null)
            {
                OnDisconnected();
                IsConnected = false;
            }
        }

        public void RequestMap()
        {
            NetOutgoingMessage message = NetClient.CreateMessage();
            message.Write((byte) NetMessage.RequestMap);
            NetClient.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public NetOutgoingMessage CreateMessage()
        {
            return NetClient.CreateMessage();
        }

        public void SendClientName(string name)
        {
            NetOutgoingMessage message = NetClient.CreateMessage();
            message.Write((byte) NetMessage.ClientName);
            message.Write(name);
            NetClient.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendMessage(NetOutgoingMessage message, NetDeliveryMethod deliveryMethod)
        {
            if (message != null)
            {
                NetClient.SendMessage(message, deliveryMethod);
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
            NetClient.Shutdown("Leaving");
            NetClient = new NetClient(_netConfig);
            NetClient.Start();
        }

        public void ShutDown()
        {
            NetClient.Shutdown("Quitting");
        }

        public void SetGameType(NetIncomingMessage msg)
        {
            _serverGameType = (GameType) msg.ReadByte();
        }

        public void SendChangeTile(int x, int z, string newTile)
        {
            var mapMgr = (MapManager) IoCManager.Resolve<IMapManager>();
            NetOutgoingMessage netMessage = NetClient.CreateMessage();
            netMessage.Write(x);
            netMessage.Write(z);
            netMessage.Write(mapMgr.GetTileIndex(newTile));
            NetClient.SendMessage(netMessage, NetDeliveryMethod.ReliableOrdered);
        }

        public NetIncomingMessage GetNetworkUpdate()
        {
            NetIncomingMessage msg;
            return (msg = NetClient.ReadMessage()) != null ? msg : null;
        }

        public string GetServerName()
        {
            return ServerName;
        }

        public string GetServerAddress()
        {
            return String.Format("{0}:{1}", NetClient.ServerConnection.RemoteEndPoint.Address, NetClient.Port);
        }

        public GameType GetGameType()
        {
            return _serverGameType;
        }
    }
}