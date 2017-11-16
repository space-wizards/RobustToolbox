using System;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network
{
    /// <summary>
    ///     A network connection from this local peer to a remote peer.
    /// </summary>
    internal class NetChannel : INetChannel
    {
        private readonly NetManager _manager;

        public INetManager NetPeer => _manager;

        /// <summary>
        ///     Creates a new instance of a NetChannel.
        /// </summary>
        /// <param name="manager">The server this channel belongs to.</param>
        /// <param name="connection">The raw NetConnection to the remote peer.</param>
        internal NetChannel(NetManager manager, NetConnection connection)
        {
            _manager = manager;
            Connection = connection;
        }

        /// <inheritdoc />
        public NetConnection Connection { get; }
        
        /// <inheritdoc />
        public long ConnectionId => Connection.RemoteUniqueIdentifier;

        /// <inheritdoc />
        public string RemoteAddress => Connection.RemoteEndPoint.Address.ToString();

        /// <inheritdoc />
        public short Ping => (short) Math.Round(Connection.AverageRoundtripTime * 1000);

        /// <inheritdoc />
        public T CreateNetMessage<T>()
            where T : NetMessage
        {
            return _manager.CreateNetMessage<T>();
        }

        /// <inheritdoc />
        public void SendMessage(NetMessage message)
        {
            _manager.ServerSendMessage(message, this);
        }
    }
}
