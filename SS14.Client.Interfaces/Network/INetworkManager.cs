using Lidgren.Network;
using SS14.Shared;
using System;

namespace SS14.Client.Interfaces.Network
{
    public interface INetworkManager
    {
        NetPeerStatistics CurrentStatistics { get; }
        NetClient NetClient { get; }
        bool IsConnected { get; }
        long UniqueId { get; }

        event EventHandler Connected;
        event EventHandler Disconnected;
        event EventHandler<IncomingNetworkMessageArgs> MessageArrived;

        void RequestMap();
        void UpdateNetwork();
        void Disconnect();
        NetOutgoingMessage CreateMessage();

        void SendMessage(NetOutgoingMessage message, NetDeliveryMethod deliveryMethod);
        void ConnectTo(string ipAddress);
        void SendClientName(string name);
    }
}