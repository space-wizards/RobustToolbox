using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lidgren.Network;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.UnitTesting
{
    public partial class RobustIntegrationTest
    {
        internal sealed class IntegrationNetManager : IClientNetManager, IServerNetManager
        {
            [Dependency] private readonly IGameTiming _gameTiming = default!;
            [Dependency] private readonly ITaskManager _taskManager = default!;
            [Dependency] private readonly IRobustSerializer _robustSerializer = default!;

            public bool IsServer { get; private set; }
            public bool IsClient => !IsServer;
            public bool IsRunning { get; private set; }
            public bool IsConnected => ChannelCount != 0;
            public NetworkStats Statistics => default;
            public IEnumerable<INetChannel> Channels => _channels.Values;
            public int ChannelCount => _channels.Count;

            private readonly Dictionary<int, IntegrationNetChannel> _channels =
                new();

            private readonly Channel<object> _messageChannel;

            public ChannelWriter<object> MessageChannelWriter => _messageChannel.Writer;

            private int _connectionUidTracker;

            private int _clientConnectingUid;

            // This isn't used for anything except a log message somewhere, so we kinda ignore it.
            public int Port => default;
            public IReadOnlyDictionary<Type, long> MessageBandwidthUsage { get; } = new Dictionary<Type, long>();

            private readonly Dictionary<Type, ProcessMessage> _callbacks = new();
            private readonly HashSet<Type> _registeredMessages = new();

            private readonly Dictionary<string, Guid> _userGuids = new Dictionary<string, Guid>();

            /// <summary>
            ///     The channel we will connect to when <see cref="ClientConnect"/> is called.
            /// </summary>
            public ChannelWriter<object>? NextConnectChannel { get; set; }

            // Used for faking NetMessage.ReadFromBuffer and WriteToBuffer.
            private readonly NetOutgoingMessage _serializationMessage = new();
            private readonly NetIncomingMessage _deserializationMessage = new();

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

            public void ResetBandwidthMetrics()
            {
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
                Reset(reason);
            }

            public void Reset(string reason)
            {
                foreach (var channel in _channels.Values.ToList())
                {
                    channel.Disconnect(reason);
                }
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

                            async Task DoConnect()
                            {
                                var writer = connect.ChannelWriter;
                                var uid = _genConnectionUid();
                                var userName = connect.Username ?? $"integration_{uid}";
                                if (!_userGuids.TryGetValue(userName, out var userId))
                                {
                                    userId = Guid.NewGuid();
                                    _userGuids.Add(userName, userId);
                                }
                                var sessionId = new NetUserId(userId);
                                var userData = new NetUserData(sessionId, userName)
                                {
                                    HWId = ImmutableArray<byte>.Empty,
                                    ModernHWIds = []
                                };

                                var args = await OnConnecting(
                                    new IPEndPoint(IPAddress.IPv6Loopback, 0),
                                    userData,
                                    LoginType.GuestAssigned);
                                if (args.IsDenied)
                                {
                                    writer.TryWrite(new DeniedConnectMessage());
                                    return;
                                }

                                writer.TryWrite(new ConfirmConnectMessage(uid, userData));
                                var channel = new IntegrationNetChannel(
                                    this,
                                    connect.ChannelWriter,
                                    uid,
                                    userData,
                                    connect.Uid);
                                _channels.Add(uid, channel);
                                Connected?.Invoke(this, new NetChannelArgs(channel));
                            }

                            _taskManager.BlockWaitOnTask(DoConnect());

                            break;
                        }

                        case DataMessage data:
                        {
                            IntegrationNetChannel? channel;
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

                            var message = DeserializeNetMessage(data);
                            message.MsgChannel = channel;
                            if (_callbacks.TryGetValue(message.GetType(), out var callback))
                            {
                                callback(message);
                            }

                            break;
                        }

                        case DisconnectMessage disconnect:
                        {
                            if (_channels.TryGetValue(disconnect.Connection, out var channel))
                            {
                                Disconnect?.Invoke(this, new NetDisconnectedArgs(channel, disconnect.Reason));
                                _channels.Remove(disconnect.Connection);
                                channel.IsConnected = false;
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

                            var channel = new IntegrationNetChannel(
                                this,
                                NextConnectChannel!,
                                _clientConnectingUid,
                                confirm.UserData,
                                confirm.AssignedUid);

                            _channels.Add(channel.ConnectionUid, channel);

                            Connected?.Invoke(this, new NetChannelArgs(channel));
                            break;
                        }

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            private async Task<NetConnectingArgs> OnConnecting(IPEndPoint ip, NetUserData userData, LoginType loginType)
            {
                var args = new NetConnectingArgs(userData, ip, loginType);
                foreach (var conn in _connectingEvent)
                {
                    await conn(args);
                }

                return args;
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

                if (recipient is DummyChannel)
                    return;

                var channel = (IntegrationNetChannel) recipient;
                channel.OtherChannel.TryWrite(SerializeNetMessage(message, channel.RemoteUid));
            }

            public void ServerSendToMany(NetMessage message, List<INetChannel> recipients)
            {
                DebugTools.Assert(IsServer);

                foreach (var recipient in recipients)
                {
                    ServerSendMessage(message, recipient);
                }
            }


            private readonly List<Func<NetConnectingArgs, Task>> _connectingEvent
                = new();

            public event Func<NetConnectingArgs, Task> Connecting
            {
                add => _connectingEvent.Add(value);
                remove => _connectingEvent.Remove(value);
            }

            public event EventHandler<NetChannelArgs>? Connected;
            public event EventHandler<NetDisconnectedArgs>? Disconnect;

            public void RegisterNetMessage<T>(ProcessMessage<T>? rxCallback = null, NetMessageAccept accept = NetMessageAccept.Both) where T : NetMessage, new()
            {
                var name = new T().MsgName;
                var thisSide = IsServer ? NetMessageAccept.Server : NetMessageAccept.Client;

                _registeredMessages.Add(typeof(T));
                if (rxCallback != null && (accept & thisSide) != 0)
                    _callbacks.Add(typeof(T), msg => rxCallback((T) msg));
            }

            public T CreateNetMessage<T>() where T : NetMessage, new()
            {
                var type = typeof(T);

                if (!_registeredMessages.Contains(type))
                {
                    throw new ArgumentException("Net message type is not registered.");
                }

                // Obsolete path for content
                if (type.GetConstructor(new[] {typeof(INetChannel)}) != null)
                {
                    return (T) Activator.CreateInstance(typeof(T), (INetChannel?) null)!;
                }
                else
                {
                    return Activator.CreateInstance<T>();
                }
            }

            public byte[]? CryptoPublicKey => null;
            public AuthMode Auth => AuthMode.Disabled;
            public Func<string, Task<NetUserId?>>? AssignUserIdCallback { get; set; }
            public IServerNetManager.NetApprovalDelegate? HandleApprovalCallback { get; set; }

            public void DisconnectChannel(INetChannel channel, string reason)
            {
                channel.Disconnect(reason);
            }

            INetChannel? IClientNetManager.ServerChannel => ServerChannel;
            public ClientConnectionState ClientConnectState => ClientConnectionState.NotConnecting;

            public event Action<ClientConnectionState>? ClientConnectStateChanged
            {
                add { }
                remove { }
            }

            private IntegrationNetChannel? ServerChannel
            {
                get
                {
                    DebugTools.Assert(IsClient);

                    return _channels.Values.FirstOrDefault();
                }
            }

            public event EventHandler<NetConnectFailArgs>? ConnectFailed;

            public void ClientConnect(string host, int port, string userNameRequest)
            {
                DebugTools.Assert(IsClient);
                DebugTools.Assert(ServerChannel == null, "Already connected.");

                if (NextConnectChannel == null)
                {
                    throw new InvalidOperationException("Didn't set a connect target!");
                }

                _clientConnectingUid = _genConnectionUid();

                NextConnectChannel.TryWrite(new ConnectMessage(MessageChannelWriter, _clientConnectingUid, userNameRequest));
            }

            public void ClientDisconnect(string reason)
            {
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

                channel.OtherChannel.TryWrite(SerializeNetMessage(message, channel.RemoteUid));
            }

            public void DispatchLocalNetMessage(NetMessage message)
            {
                if (_callbacks.TryGetValue(message.GetType(), out var callback))
                {
                    callback(message);
                }
            }

            private DataMessage SerializeNetMessage(NetMessage netMessage, int remoteUid)
            {
                byte[] pooledBuffer;
                int length;
                lock (_serializationMessage)
                {
                    netMessage.WriteToBuffer(_serializationMessage, _robustSerializer);
                    length = _serializationMessage.LengthBytes;
                    pooledBuffer = ArrayPool<byte>.Shared.Rent(length);
                    _serializationMessage.Data.AsSpan(0, length).CopyTo(pooledBuffer);
                    _serializationMessage.Position = 0;
                    _serializationMessage.LengthBytes = 0;
                }

                return new DataMessage(pooledBuffer, length, netMessage.GetType(), remoteUid);
            }

            private NetMessage DeserializeNetMessage(DataMessage message)
            {
                var buffer = message.PooledNetBuffer;
                var netMessage = (NetMessage) Activator.CreateInstance(message.MessageType)!;
                lock (_deserializationMessage)
                {
                    _deserializationMessage.m_data = buffer;
                    _deserializationMessage.LengthBytes = message.Length;
                    _deserializationMessage.Position = 0;

                    netMessage.ReadFromBuffer(_deserializationMessage, _robustSerializer);

                    _deserializationMessage.m_data = null;
                }

                ArrayPool<byte>.Shared.Return(buffer);
                return netMessage;
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

                public bool IsConnected { get; set; }
                public NetUserData UserData { get; }
                // integration tests don't simulate serializer handshake so this is always true.
                public bool IsHandshakeComplete => true;
                public int CurrentMtu => 1000; // Arbitrary.

                // TODO: Should this port value make sense?
                // See also the DummyChannel class
                public IPEndPoint RemoteEndPoint { get; } = new(IPAddress.Loopback, 1212);
                public NetUserId UserId => UserData.UserId;
                public string UserName => UserData.UserName;
                public LoginType AuthType => LoginType.GuestAssigned;
                public TimeSpan RemoteTimeOffset => TimeSpan.Zero; // TODO: Fix this
                public TimeSpan RemoteTime => _owner._gameTiming.RealTime + RemoteTimeOffset;
                public short Ping => default;

                public IntegrationNetChannel(
                    IntegrationNetManager owner,
                    ChannelWriter<object> otherChannel,
                    int uid,
                    NetUserData userData,
                    int remoteUid)
                {
                    _owner = owner;
                    ConnectionUid = uid;
                    UserData = userData;
                    OtherChannel = otherChannel;
                    IsConnected = true;
                    RemoteUid = remoteUid;
                }

                public T CreateNetMessage<T>() where T : NetMessage, new()
                {
                    return _owner.CreateNetMessage<T>();
                }

                public void SendMessage(NetMessage message)
                {
                    _owner.ServerSendMessage(message, this);
                }

                public void Disconnect(string reason)
                {
                    OtherChannel.TryWrite(new DisconnectMessage(RemoteUid, reason));
                    _owner.MessageChannelWriter.TryWrite(new DisconnectMessage(ConnectionUid, reason));
                }

                public void Disconnect(string reason, bool sendBye)
                {
                    // Don't handle bye sending in here I guess.
                    Disconnect(reason);
                }
            }

            private sealed class ConnectMessage
            {
                public ConnectMessage(ChannelWriter<object> channelWriter, int uid, string? username)
                {
                    ChannelWriter = channelWriter;
                    Uid = uid;
                    Username = username;
                }

                public ChannelWriter<object> ChannelWriter { get; }
                public int Uid { get; }
                public string? Username { get; }
            }

            private sealed class ConfirmConnectMessage
            {
                public ConfirmConnectMessage(int assignedUid, NetUserData userData)
                {
                    AssignedUid = assignedUid;
                    UserData = userData;
                }

                public int AssignedUid { get; }
                public NetUserData UserData { get; }
            }

            private sealed class DeniedConnectMessage
            {
            }

            private sealed record DataMessage(byte[] PooledNetBuffer, int Length, Type MessageType, int Connection);

            private sealed class DisconnectMessage
            {
                public DisconnectMessage(int connection, string reason)
                {
                    Reason = reason;
                    Connection = connection;
                }

                public readonly string Reason;
                public int Connection { get; }
            }
        }
    }
}
