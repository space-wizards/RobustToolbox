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
        private readonly NetConnection _connection;

        /// <inheritdoc />
        public long ConnectionId => _connection.RemoteUniqueIdentifier;

        /// <inheritdoc />
        public INetManager NetPeer => _manager;

        /// <inheritdoc />
        public short Ping => (short) Math.Round(_connection.AverageRoundtripTime * 1000);

        /// <inheritdoc />
        public string RemoteAddress => _connection.RemoteEndPoint.Address.ToString();

        /// <summary>
        ///     Creates a new instance of a NetChannel.
        /// </summary>
        /// <param name="manager">The server this channel belongs to.</param>
        /// <param name="connection">The raw NetConnection to the remote peer.</param>
        internal NetChannel(NetManager manager, NetConnection connection)
        {
            _manager = manager;
            _connection = connection;
        }

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

        /// <inheritdoc />
        public void Disconnect(string reason)
        {
            if (_connection.Status == NetConnectionStatus.Connected)
                _connection.Disconnect(reason);
        }
    }
}
