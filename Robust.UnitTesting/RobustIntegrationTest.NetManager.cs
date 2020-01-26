using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Channels;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Robust.UnitTesting
{
    public partial class RobustIntegrationTest
    {
        internal sealed class IntegrationNetManager : IClientNetManager, IServerNetManager
        {
            public bool IsServer { get; private set; }
            public bool IsClient => !IsServer;
            public bool IsRunning { get; private set; }
            public bool IsConnected => ChannelCount != 0;
            public NetworkStats Statistics => default;
            public IEnumerable<INetChannel> Channels => _channels.Values;
            public int ChannelCount => _channels.Count;

            private readonly Dictionary<int, IntegrationNetChannel> _channels =
                new Dictionary<int, IntegrationNetChannel>();

            private readonly Channel<object> _messageChannel;

            public ChannelWriter<object> MessageChannelWriter => _messageChannel.Writer;

            private int _connectionUidTracker;

            private int _clientConnectingUid;

            // This isn't used for anything except a log message somewhere, so we kinda ignore it.
            public int Port => default;

            private readonly Dictionary<Type, ProcessMessage> _callbacks = new Dictionary<Type, ProcessMessage>();

            /// <summary>
            ///     The channel we will connect to when <see cref="ClientConnect"/> is called.
            /// </summary>
            public ChannelWriter<object> NextConnectChannel { get; set; }

            private int _genConnectionUid()
            {
                return ++_connectionUidTracker;
            }

            public IntegrationNetManager()
            {
                _messageChannel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
                {
                    SingleReader = true
                });
            }

            public void Initialize(bool isServer)
            {
                IsServer = isServer;
            }

            public void StartServer()
            {
                DebugTools.Assert(IsServer);
                if (IsRunning)
                {
                    throw new InvalidOperationException("Already running!");
                }

                IsRunning = true;
            }

            public void Shutdown(string reason)
            {
                foreach (var channel in _channels.Values.ToList())
                {
                    channel.Disconnect(reason);
                }

                _channels.Clear();
            }

            public void ProcessPackets()
            {
                while (_messageChannel.Reader.TryRead(out var item))
                {
                    switch (item)
                    {
                        case ConnectMessage connect:
                        {
                            DebugTools.Assert(IsServer);

                            var writer = connect.ChannelWriter;

                            var uid = _genConnectionUid();
                            var sessionId = new NetSessionId($"integration_{uid}");

                            var connectArgs =
                                new NetConnectingArgs(sessionId, new IPEndPoint(IPAddress.IPv6Loopback, 0));
                            Connecting?.Invoke(this, connectArgs);
                            if (connectArgs.Deny)
                            {
                                writer.TryWrite(new DeniedConnectMessage());
                                continue;
                            }

                            writer.TryWrite(new ConfirmConnectMessage(uid, sessionId));
                            var channel = new IntegrationNetChannel(this, connect.ChannelWriter, uid, sessionId, connect.Uid);
                            _channels.Add(uid, channel);
                            Connected?.Invoke(this, new NetChannelArgs(channel));
                            break;
                        }

                        case DataMessage data:
                        {
                            IntegrationNetChannel channel;
                            if (IsServer)
                            {
                                if (!_channels.TryGetValue(data.Connection, out channel))
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (ServerChannel == null || data.Connection != ServerChannel.ConnectionUid)
                                {
                                    continue;
                                }

                                channel = ServerChannel;
                            }

                            var message = data.Message;
                            message.MsgChannel = channel;
                            if (_callbacks.TryGetValue(message.GetType(), out var callback))
                            {
                                callback(message);
                            }

                            break;
                        }

                        case DisconnectMessage disconnect:
                        {
                            if (IsServer)
                            {
                                if (_channels.TryGetValue(disconnect.Connection, out var channel))
                                {
                                    Disconnect?.Invoke(this, new NetChannelArgs(channel));

                                    _channels.Remove(disconnect.Connection);
                                }
                            }
                            else
                            {
                                _channels.Clear();
                            }

                            break;
                        }

                        case DeniedConnectMessage _:
                        {
                            DebugTools.Assert(IsClient);

                            ConnectFailed?.Invoke(this, new NetConnectFailArgs("I didn't implement a deny reason!"));
                            break;
                        }

                        case ConfirmConnectMessage confirm:
                        {
                            DebugTools.Assert(IsClient);

                            var channel = new IntegrationNetChannel(this, NextConnectChannel, _clientConnectingUid,
                                confirm.SessionId, confirm.AssignedUid);

                            _channels.Add(channel.ConnectionUid, channel);

                            Connected?.Invoke(this, new NetChannelArgs(channel));
                            break;
                        }

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            public void ServerSendToAll(NetMessage message)
            {
                DebugTools.Assert(IsServer);

                foreach (var channel in _channels.Values)
                {
                    ServerSendMessage(message, channel);
                }
            }

            public void ServerSendMessage(NetMessage message, INetChannel recipient)
            {
                DebugTools.Assert(IsServer);

                var channel = (IntegrationNetChannel) recipient;
                channel.OtherChannel.TryWrite(new DataMessage(message, channel.RemoteUid));
            }

            public void ServerSendToMany(NetMessage message, List<INetChannel> recipients)
            {
                DebugTools.Assert(IsServer);

                foreach (var recipient in recipients)
                {
                    ServerSendMessage(message, recipient);
                }
            }

            public event EventHandler<NetConnectingArgs> Connecting;
            public event EventHandler<NetChannelArgs> Connected;
            public event EventHandler<NetChannelArgs> Disconnect;

            public void RegisterNetMessage<T>(string name, ProcessMessage<T> rxCallback = null) where T : NetMessage
            {
                if (rxCallback != null)
                    _callbacks.Add(typeof(T), msg => rxCallback((T) msg));
            }

            public T CreateNetMessage<T>() where T : NetMessage
            {
                return (T) Activator.CreateInstance(typeof(T), (INetChannel) null);
            }

            public void DisconnectChannel(INetChannel channel, string reason)
            {
                channel.Disconnect(reason);
            }

            INetChannel IClientNetManager.ServerChannel => ServerChannel;

            private IntegrationNetChannel ServerChannel
            {
                get
                {
                    DebugTools.Assert(IsClient);

                    return _channels.Values.FirstOrDefault();
                }
            }

            public event EventHandler<NetConnectFailArgs> ConnectFailed;

            public void ClientConnect(string host, int port, string userNameRequest)
            {
                DebugTools.Assert(IsClient);

                if (NextConnectChannel == null)
                {
                    throw new InvalidOperationException("Didn't set a connect target!");
                }

                _clientConnectingUid = _genConnectionUid();

                NextConnectChannel.TryWrite(new ConnectMessage(MessageChannelWriter, _clientConnectingUid));
            }

            public void ClientDisconnect(string reason)
            {
                DebugTools.Assert(IsClient);
                Disconnect?.Invoke(this, new NetChannelArgs(ServerChannel));
                Shutdown(reason);
            }

            public void ClientSendMessage(NetMessage message)
            {
                DebugTools.Assert(IsClient);

                var channel = ServerChannel;
                if (channel == null)
                {
                    throw new InvalidOperationException("Not connected.");
                }

                channel.OtherChannel.TryWrite(new DataMessage(message, channel.RemoteUid));
            }

            private sealed class IntegrationNetChannel : INetChannel
            {
                private readonly IntegrationNetManager _owner;

                // This is the channel going to the other integration manager.
                public ChannelWriter<object> OtherChannel { get; }

                public INetManager NetPeer => _owner;

                public int RemoteUid { get; }
                public int ConnectionUid { get; }
                long INetChannel.ConnectionId => ConnectionUid;

                // TODO: Should this port value make sense?
                public IPEndPoint RemoteEndPoint { get; } = new IPEndPoint(IPAddress.Loopback, 1212);
                public NetSessionId SessionId { get; }
                public short Ping => default;

                public IntegrationNetChannel(IntegrationNetManager owner, ChannelWriter<object> otherChannel, int uid,
                    NetSessionId sessionId)
                {
                    _owner = owner;
                    ConnectionUid = uid;
                    SessionId = sessionId;
                    OtherChannel = otherChannel;
                }

                public IntegrationNetChannel(IntegrationNetManager owner, ChannelWriter<object> otherChannel, int uid,
                    NetSessionId sessionId, int remoteUid) : this(owner, otherChannel, uid, sessionId)
                {
                    RemoteUid = uid;
                }

                public T CreateNetMessage<T>() where T : NetMessage
                {
                    return _owner.CreateNetMessage<T>();
                }

                public void SendMessage(NetMessage message)
                {
                    _owner.ServerSendMessage(message, this);
                }

                public void Disconnect(string reason)
                {
                    OtherChannel.TryWrite(new DisconnectMessage(RemoteUid));
                }
            }

            private sealed class ConnectMessage
            {
                public ConnectMessage(ChannelWriter<object> channelWriter, int uid)
                {
                    ChannelWriter = channelWriter;
                    Uid = uid;
                }

                public ChannelWriter<object> ChannelWriter { get; }
                public int Uid { get; }
            }

            private sealed class ConfirmConnectMessage
            {
                public ConfirmConnectMessage(int assignedUid, NetSessionId sessionId)
                {
                    AssignedUid = assignedUid;
                    SessionId = sessionId;
                }

                public int AssignedUid { get; }
                public NetSessionId SessionId { get; }
            }

            private sealed class DeniedConnectMessage
            {
            }

            private sealed class DataMessage
            {
                public DataMessage(NetMessage message, int connection)
                {
                    Message = message;
                    Connection = connection;
                }

                public NetMessage Message { get; }
                public int Connection { get; }
            }

            private sealed class DisconnectMessage
            {
                public DisconnectMessage(int connection)
                {
                    Connection = connection;
                }

                public int Connection { get; }
            }
        }
    }
}
