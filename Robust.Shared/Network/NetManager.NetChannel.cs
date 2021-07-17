using System;
using System.Collections.Immutable;
using System.Net;
using Lidgren.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Network
{
    public partial class NetManager
    {
        private class NetChannel : INetChannel
        {
            private readonly NetManager _manager;
            private readonly NetConnection _connection;

            /// <inheritdoc />
            [ViewVariables]
            public long ConnectionId => _connection.RemoteUniqueIdentifier;

            /// <inheritdoc />
            [ViewVariables]
            public INetManager NetPeer => _manager;

            [ViewVariables] public string UserName => UserData.UserName;
            [ViewVariables] public LoginType AuthType { get; }
            [ViewVariables] public TimeSpan RemoteTimeOffset => TimeSpan.FromSeconds(_connection.RemoteTimeOffset);
            [ViewVariables] public TimeSpan RemoteTime => _manager._timing.RealTime + RemoteTimeOffset;

            /// <inheritdoc />
            [ViewVariables]
            public short Ping => (short) Math.Round(_connection.AverageRoundtripTime * 1000);

            /// <inheritdoc />
            [ViewVariables]
            public bool IsConnected => _connection.Status == NetConnectionStatus.Connected;

            /// <inheritdoc />
            [ViewVariables]
            public IPEndPoint RemoteEndPoint => _connection.RemoteEndPoint;

            /// <summary>
            ///     Exposes the lidgren connection.
            /// </summary>
            public NetConnection Connection => _connection;

            [ViewVariables] public NetUserId UserId => UserData.UserId;
            [ViewVariables] public NetUserData UserData { get; }

            // Only used on server, contains the encryption to use for this channel.
            public NetEncryption? Encryption { get; set; }

            /// <summary>
            ///     Creates a new instance of a NetChannel.
            /// </summary>
            /// <param name="manager">The server this channel belongs to.</param>
            /// <param name="connection">The raw NetConnection to the remote peer.</param>
            internal NetChannel(NetManager manager, NetConnection connection, NetUserData userData,
                LoginType loginType)
            {
                _manager = manager;
                _connection = connection;
                AuthType = loginType;
                UserData = userData;
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
                if (_manager.IsClient)
                {
                    _manager.ClientSendMessage(message);
                    return;
                }

                _manager.ServerSendMessage(message, this);
            }

            /// <inheritdoc />
            public void Disconnect(string reason)
            {
                if (_connection.Status == NetConnectionStatus.Connected)
                    _connection.Disconnect(reason);
            }

            public override string ToString()
            {
                return $"{RemoteEndPoint}/{UserId}";
            }
        }
    }
}
