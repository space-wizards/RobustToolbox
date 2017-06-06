using Lidgren.Network;
using SS14.Server.Interfaces.Configuration;
using SS14.Server.Interfaces.Network;
using SS14.Shared.IoC;
using System.Collections.Generic;
using System;
using System.Threading;

namespace SS14.Server.Network
{
    [IoCTarget]
    public class SS14NetServer : NetServer, ISS14NetServer
    {
        public SS14NetServer()
            : base(LoadNetPeerConfig())
        {
        }

        #region ISS13NetServer Members

        public void SendToAll(NetOutgoingMessage message)
        {
            SendToAll(message, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendMessage(NetOutgoingMessage message, NetConnection client)
        {
            SendMessage(message, client, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendToMany(NetOutgoingMessage message, List<NetConnection> recipients)
        {
             SendMessage(message, recipients, NetDeliveryMethod.ReliableOrdered, 0);
        }

        #endregion

        public static NetPeerConfiguration LoadNetPeerConfig()
        {
            var _config = new NetPeerConfiguration("SS13_NetTag");
            _config.Port = IoCManager.Resolve<IServerConfigurationManager>().Port;
#if DEBUG
            _config.ConnectionTimeout = 30000f;
#endif

            return _config;
        }

        public void SendMessage(NetOutgoingMessage message, List<NetConnection> recipients, NetDeliveryMethod method, int sequenceChannel)
        {
            foreach (NetConnection recipient in recipients)
                SendMessage(message, recipient, NetDeliveryMethod.ReliableOrdered, sequenceChannel);
        }

        // acc to Lidgren code comment: Call this to register a callback for when a new message arrives
        // we aren't currently using it, so it's left as this exception
        public void RegisterReceivedCallback(SendOrPostCallback callback)
        {
            throw new NotImplementedException();
        }
    }
}
