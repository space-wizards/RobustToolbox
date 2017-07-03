using System;
using Lidgren.Network;

namespace SS14.Shared.Network
{
    /// <summary>
    /// A network connection from this local peer to a remote peer. This class
    /// sends and receives as well as processes NetMessages.
    /// </summary>
    public class NetChannel
    {
        [Obsolete]
        public NetConnection Connection { get; }

        public int NetworkId { get; set; }
        public long UUID { get; set; }

        public string PlayerName { get; set; }
        public ClientStatus Status { get; set; }
        public ushort MobID { get; set; }

        private NetworkServer _server;

        public NetChannel(NetworkServer server, NetConnection connection)
        {
            _server = server;
            Connection = connection;
        }

        public T CreateMessage<T>()
            where T : NetMessage
        {
            return (T)Activator.CreateInstance(typeof(T), this);
        }

        public void SendMessage(NetMessage message)
        {
            var packet = _server.CreateMessage();

            if (_server.TryFindStringId(message.Name, out int msgID))
            {
                packet.Write((byte)msgID);
                message.WriteToBuffer(packet);
                _server.SendMessage(packet, Connection);
                return;
            }
            throw new Exception($"[NET] No string in table with name {message.Name}. Was it registered?");
        }
    }
}
