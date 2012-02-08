using System;
using Lidgren.Network;
using SS13_Shared;

namespace ClientInterfaces.Network
{
    public interface INetworkManager
    {
        NetPeerStatistics CurrentStatistics { get; }
        bool IsConnected { get; }

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
