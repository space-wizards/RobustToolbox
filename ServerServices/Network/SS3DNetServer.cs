using Lidgren.Network;
using SS13.IoC;
using ServerInterfaces.Configuration;
using ServerInterfaces.Network;

namespace ServerServices.Network
{
    public class SS13NetServer : NetServer, ISS13NetServer
    {
        public SS13NetServer()
            :base(LoadNetPeerConfig())
        {}

        public void SendToAll(NetOutgoingMessage message)
        {
            SendToAll(message, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendMessage(NetOutgoingMessage message, NetConnection client)
        {
            SendMessage(message, client, NetDeliveryMethod.ReliableOrdered);
        }

        public static NetPeerConfiguration LoadNetPeerConfig()
        {
            var _config = new NetPeerConfiguration("SS13_NetTag");
            _config.Port = IoCManager.Resolve<IConfigurationManager>().Port;
            return _config;
        }
    }
}
