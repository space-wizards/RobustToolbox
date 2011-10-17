using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerInterfaces;

using Lidgren.Network;

namespace ServerServices
{
    public class SS3DNetServer : NetServer, IService
    {
        private static SS3DNetServer singleton;
        public static SS3DNetServer Singleton
        {
            get
            {
                if (singleton == null)
                    throw new TypeInitializationException("Singleton not initialized.", null);
                return singleton;

            }
        }

        public SS3DNetServer(NetPeerConfiguration netConfig)
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
