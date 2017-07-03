using System;
using System.Collections.Generic;
using Lidgren.Network;
using SS14.Shared.Network;

namespace SS14.Shared.Interfaces.Network
{
    public interface INetworkServer
    {
        void Initialize(bool isServer);
        void ProcessPackets();

        // IF CLIENT
        NetChannel GetServerChannel();
        NetChannel GetChannel(NetConnection connection);

        //TODO: Encapsulate this
        NetPeerStatistics Statistics { get; }

        IEnumerable<NetChannel> Connections { get; }

        int ConnectionCount { get; }

        void SendToAll(NetMessage message);
        void SendMessage(NetMessage message, NetChannel client);
        void SendToMany(NetMessage message, List<NetChannel> recipients);

        #region StringTable
        
        void RegisterNetMessage<T>(string name, int id, NetMessage.ProcessMessage func = null)
            where T : NetMessage;

        #endregion

        event OnConnectingEvent OnConnecting;
        event OnConnectedEvent OnConnected;
        event OnDisconnectEvent OnDisconnect;

        #region Obsolete

        [Obsolete]
        NetServer Server { get; }

        [Obsolete]
        void SendToAll(NetOutgoingMessage message);
        [Obsolete]
        void SendMessage(NetOutgoingMessage message, NetConnection client);
        [Obsolete]
        void SendToMany(NetOutgoingMessage message, List<NetConnection> recipients);


        #endregion
    }
}
