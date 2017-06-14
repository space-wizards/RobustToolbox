using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.Interfaces.Network
{
    public interface INetworkManager : IIoCInterface
    {
        void Initialize();
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
