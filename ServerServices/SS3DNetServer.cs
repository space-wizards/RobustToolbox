using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using ServerInterfaces;

using Lidgren.Network;

namespace ServerServices
{
    public class SS13NetServer : NetServer, IService
    {
        private static SS13NetServer singleton;
        public static SS13NetServer Singleton
        {
            get
            {
                if (singleton == null)
                    throw new TypeInitializationException("Singleton not initialized.", null);
                return singleton;

            }
        }

        public SS13NetServer(NetPeerConfiguration netConfig)
            :base(netConfig)
        {
            singleton = this;
        }

        public void SendToAll(NetOutgoingMessage message)
        {
            SendToAll(message, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendMessage(NetOutgoingMessage message, NetConnection client)
        {
            SendMessage(message, client, NetDeliveryMethod.ReliableOrdered);
        }

        public ServerServiceType ServiceType
        {
            get { return ServerServiceType.NetServer; }
        }
    }
}
