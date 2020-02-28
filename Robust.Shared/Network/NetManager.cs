using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using Robust.Shared.Configuration;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Network
{
    /// <summary>
    ///     Callback for registered NetMessages.
    /// </summary>
    /// <param name="message">The message received.</param>
    public delegate void ProcessMessage(NetMessage message);

    /// <summary>
    ///     Callback for registered NetMessages.
    /// </summary>
    /// <param name="message">The message received.</param>
    public delegate void ProcessMessage<in T>(T message) where T : NetMessage;

    /// <summary>
    ///     Manages all network connections and packet IO.
    /// </summary>
    public partial class NetManager : IClientNetManager, IServerNetManager, IDisposable
    {
        private readonly Dictionary<Type, ProcessMessage> _callbacks = new Dictionary<Type, ProcessMessage>();

        /// <summary>
        ///     Holds the synced lookup table of NetConnection -> NetChannel
        /// </summary>
        private readonly Dictionary<NetConnection, NetChannel> _channels = new Dictionary<NetConnection, NetChannel>();

        private readonly Dictionary<NetConnection, NetSessionId> _assignedSessions =
            new Dictionary<NetConnection, NetSessionId>();

        // Used for processing incoming net messages.
        private readonly (Func<INetChannel, NetMessage>, Type)[] _netMsgFunctions = new (Func<INetChannel, NetMessage>, Type)[256];

        // Used for processing outgoing net messages.
        private readonly Dictionary<Type, Func<NetMessage>> _blankNetMsgFunctions = new Dictionary<Type, Func<NetMessage>>();

#pragma warning disable 649
        [Dependency] private readonly IConfigurationManager _config;
#pragma warning restore 649

        /// <summary>
        ///     Holds lookup table for NetMessage.Id -> NetMessage.Type
        /// </summary>
        private readonly Dictionary<string, Type> _messages = new Dictionary<string, Type>();

        /// <summary>
        /// The StringTable for transforming packet Ids to Packet name.
        /// </summary>
        private readonly StringTable _strings = new StringTable();

        /// <summary>
        ///     The list of network peers we are listening on.
        /// </summary>
        private readonly List<NetPeer> _netPeers = new List<NetPeer>();

        // Client connect happens during status changed and such callbacks, so we need to defer deletion of these.
        private readonly List<NetPeer> _toCleanNetPeers = new List<NetPeer>();

        /// <inheritdoc />
        public int Port => _config.GetCVar<int>("net.port");

        /// <inheritdoc />
        public bool IsServer { get; private set; }

        /// <inheritdoc />
        public bool IsClient => !IsServer;

        /// <inheritdoc />
        public bool IsConnected
        {
            get
            {
                foreach (var p in _netPeers)
                {
                    if (p.ConnectionsCount > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsRunning => _netPeers.Count != 0;

        private ClientConnectionState _clientConnectionState;

        public NetworkStats Statistics
        {
            get
            {
                var sentPackets = 0;
                var sentBytes = 0;
                var recvPackets = 0;
                var recvBytes = 0;

                foreach (var peer in _netPeers)
                {
                    var netPeerStatistics = peer.Statistics;
                    sentPackets += netPeerStatistics.SentPackets;
                    sentBytes += netPeerStatistics.SentBytes;
                    recvPackets += netPeerStatistics.ReceivedPackets;
                    recvBytes += netPeerStatistics.ReceivedBytes;
                }

                return new NetworkStats(sentBytes, recvBytes, sentPackets, recvPackets);
            }
        }

        /// <inheritdoc />
        public IEnumerable<INetChannel> Channels => _channels.Values;

        /// <inheritdoc />
        public int ChannelCount => _channels.Count;

        public IReadOnlyDictionary<Type, ProcessMessage> CallbackAudit => _callbacks;

        /// <inheritdoc />
        public INetChannel ServerChannel
        {
            get
            {
                DebugTools.Assert(IsClient);

                if (_netPeers.Count == 0)
                {
                    return null;
                }

                var peer = _netPeers[0];
                if (peer.ConnectionsCount == 0)
                {
                    return null;
                }

                TryGetChannel(peer.Connections[0], out var channel);
                return channel;
            }
        }

        private bool _initialized;

        /// <inheritdoc />
        public void Initialize(bool isServer)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("NetManager has already been initialized.");
            }

            IsServer = isServer;

            _config.RegisterCVar("net.port", 1212, CVar.ARCHIVE);

            _config.RegisterCVar("net.sendbuffersize", 131071, CVar.ARCHIVE);
            _config.RegisterCVar("net.receivebuffersize", 131071, CVar.ARCHIVE);

            if (!isServer)
            {
                _config.RegisterCVar("net.server", "127.0.0.1", CVar.ARCHIVE);
                _config.RegisterCVar("net.updaterate", 20, CVar.ARCHIVE);
                _config.RegisterCVar("net.cmdrate", 30, CVar.ARCHIVE);
                _config.RegisterCVar("net.rate", 10240, CVar.REPLICATED | CVar.ARCHIVE);
            }
            else
            {
                // That's comma-separated, btw.
                _config.RegisterCVar("net.bindto", "0.0.0.0,::", CVar.ARCHIVE);
                _config.RegisterCVar("net.dualstack", false, CVar.ARCHIVE);
            }

#if DEBUG
            _config.RegisterCVar("net.fakeloss", 0.0f, CVar.CHEAT, _fakeLossChanged);
            _config.RegisterCVar("net.fakelagmin", 0.0f, CVar.CHEAT, _fakeLagMinChanged);
            _config.RegisterCVar("net.fakelagrand", 0.0f, CVar.CHEAT, _fakeLagRandomChanged);
#endif

            _strings.Initialize(this, () => { OnConnected(ServerChannel); });

            _initialized = true;
        }

        public void StartServer()
        {
            DebugTools.Assert(IsServer);
            DebugTools.Assert(!IsRunning);

            var binds = _config.GetCVar<string>("net.bindto").Split(',');
            var dualStack = _config.GetCVar<bool>("net.dualstack");

            var foundIpv6 = false;

            foreach (var bindAddress in binds)
            {
                if (!IPAddress.TryParse(bindAddress.Trim(), out var address))
                {
                    throw new InvalidOperationException("Not a valid IPv4 or IPv6 address");
                }

                var config = _getBaseNetPeerConfig();
                config.LocalAddress = address;
                config.Port = Port;
                config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

                if (address.AddressFamily == AddressFamily.InterNetworkV6 && dualStack)
                {
                    foundIpv6 = true;
                    config.DualStack = true;
                }

                var peer = IsServer ? (NetPeer) new NetServer(config) : new NetClient(config);
                peer.Start();
                _netPeers.Add(peer);
            }

            if (_netPeers.Count == 0)
            {
                Logger.WarningS("net",
                    "Exactly 0 addresses have been bound to, nothing will be able to connect to the server.");
            }

            if (!foundIpv6 && dualStack)
            {
                Logger.WarningS("net",
                    "IPv6 Dual Stack is enabled but no IPv6 addresses have been bound to. This will not work.");
            }
        }

        public void Dispose()
        {
            Shutdown("Network manager getting disposed.");
        }

        /// <inheritdoc />
        public void Shutdown(string reason)
        {
            foreach (var kvChannel in _channels)
                DisconnectChannel(kvChannel.Value, reason);

            // request shutdown of the netPeer
            _netPeers.ForEach(p => p.Shutdown(reason));
            _netPeers.Clear();

            // wait for the network thread to finish its work (like flushing packets and gracefully disconnecting)
            // Lidgren does not expose the thread, so we can't join or or anything
            // pretty much have to poll every so often and wait for it to finish before continuing
            // when the network thread is finished, it will change status from ShutdownRequested to NotRunning
            while (_netPeers.Any(p => p.Status == NetPeerStatus.ShutdownRequested))
            {
                // sleep the thread for an arbitrary length so it isn't spinning in the while loop as much
                Thread.Sleep(50);
            }

            _strings.Reset();

            _cancelConnectTokenSource?.Cancel();
            _clientConnectionState = ClientConnectionState.NotConnecting;
        }

        public void ProcessPackets()
        {
            foreach (var peer in _netPeers)
            {
                NetIncomingMessage msg;
                var recycle = true;
                while ((msg = peer.ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                            Logger.DebugS("net", "{0}: {1}", peer.Configuration.LocalAddress, msg.ReadString());
                            break;

                        case NetIncomingMessageType.DebugMessage:
                            Logger.InfoS("net", "{0}: {1}", peer.Configuration.LocalAddress, msg.ReadString());
                            break;

                        case NetIncomingMessageType.WarningMessage:
                            Logger.WarningS("net", "{0}: {1}", peer.Configuration.LocalAddress, msg.ReadString());
                            break;

                        case NetIncomingMessageType.ErrorMessage:
                            Logger.ErrorS("net", "{0}: {1}", peer.Configuration.LocalAddress, msg.ReadString());
                            break;

                        case NetIncomingMessageType.ConnectionApproval:
                            HandleApproval(msg);
                            break;

                        case NetIncomingMessageType.Data:
                            recycle = DispatchNetMessage(msg);
                            break;

                        case NetIncomingMessageType.StatusChanged:
                            HandleStatusChanged(msg);
                            break;

                        default:
                            Logger.WarningS("net",
                                "{0}: Unhandled incoming packet type from {1}: {2}",
                                peer.Configuration.LocalAddress,
                                msg.SenderConnection.RemoteEndPoint,
                                msg.MessageType);
                            break;
                    }

                    if (recycle)
                    {
                        peer.Recycle(msg);
                    }
                }
            }

            if (_toCleanNetPeers.Count != 0)
            {
                foreach (var peer in _toCleanNetPeers)
                {
                    _netPeers.Remove(peer);
                }
            }
        }

        /// <inheritdoc />
        public void ClientDisconnect(string reason)
        {
            DebugTools.Assert(IsClient, "Should never be called on the server.");
            Disconnect?.Invoke(this, new NetChannelArgs(ServerChannel));
            Shutdown(reason);
        }

        private NetPeerConfiguration _getBaseNetPeerConfig()
        {
            var netConfig = new NetPeerConfiguration("SS14_NetTag");

            // ping the client 4 times every second.
            netConfig.PingInterval = 0.25f;

            netConfig.SendBufferSize = _config.GetCVar<int>("net.sendbuffersize");
            netConfig.ReceiveBufferSize = _config.GetCVar<int>("net.receivebuffersize");

            if (IsServer)
            {
                netConfig.MaximumConnections = _config.GetCVar<int>("game.maxplayers");
            }

#if DEBUG
            //Simulate Latency
            netConfig.SimulatedLoss = _config.GetCVar<float>("net.fakeloss");
            netConfig.SimulatedMinimumLatency = _config.GetCVar<float>("net.fakelagmin");
            netConfig.SimulatedRandomLatency = _config.GetCVar<float>("net.fakelagrand");

            netConfig.ConnectionTimeout = 30000f;
#endif
            return netConfig;
        }

#if DEBUG
        private void _fakeLossChanged(float newValue)
        {
            foreach (var peer in _netPeers)
            {
                peer.Configuration.SimulatedLoss = newValue;
            }
        }

        private void _fakeLagMinChanged(float newValue)
        {
            foreach (var peer in _netPeers)
            {
                peer.Configuration.SimulatedMinimumLatency = newValue;
            }
        }

        private void _fakeLagRandomChanged(float newValue)
        {
            foreach (var peer in _netPeers)
            {
                peer.Configuration.SimulatedRandomLatency = newValue;
            }
        }
#endif

        /// <summary>
        ///     Gets the NetChannel of a peer NetConnection.
        /// </summary>
        /// <param name="connection">The raw connection of the peer.</param>
        /// <returns>The NetChannel of the peer.</returns>
        private INetChannel GetChannel(NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (_channels.TryGetValue(connection, out NetChannel channel))
                return channel;

            throw new NetManagerException("There is no NetChannel for this NetConnection.");
        }

        private bool TryGetChannel(NetConnection connection, out INetChannel channel)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (_channels.TryGetValue(connection, out var channelInstance))
            {
                channel = channelInstance;
                return true;
            }

            channel = default;
            return false;
        }

        private void HandleStatusChanged(NetIncomingMessage msg)
        {
            var sender = msg.SenderConnection;
            msg.ReadByte();
            var reason = msg.ReadString();
            Logger.DebugS("net", $"{sender.RemoteEndPoint}: Status changed to {sender.Status}, reason: {reason}");

            if (_awaitingStatusChange.TryGetValue(sender, out var resume))
            {
                _awaitingStatusChange.Remove(sender);
                resume.Item1.Dispose();
                resume.Item2.SetResult(reason);
                return;
            }

            switch (sender.Status)
            {
                case NetConnectionStatus.Connected:
                    if (IsServer)
                    {
                        HandleHandshake(sender);
                    }

                    break;

                case NetConnectionStatus.Disconnected:
                    if (_awaitingData.TryGetValue(sender, out var awaitInfo))
                    {
                        awaitInfo.Item1.Dispose();
                        awaitInfo.Item2.TrySetException(
                            new ClientDisconnectedException($"Disconnected: {reason}"));
                        _awaitingData.Remove(sender);
                    }

                    if (_channels.ContainsKey(sender))
                    {
                        HandleDisconnect(sender, reason);
                    }

                    break;
            }
        }

        private void HandleApproval(NetIncomingMessage message)
        {
            // TODO: Maybe preemptively refuse connections here in some cases?
            if (message.SenderConnection.Status != NetConnectionStatus.RespondedAwaitingApproval)
            {
                // This can happen if the approval message comes in after the state changes to disconnected.
                // In that case just ignore it.
                return;
            }
            message.SenderConnection.Approve();
        }

        private async void HandleHandshake(NetConnection connection)
        {
            string requestedUsername;
            try
            {
                var userNamePacket = await AwaitData(connection);
                requestedUsername = userNamePacket.ReadString();
            }
            catch (ClientDisconnectedException)
            {
                return;
            }

            var (nameValid, invalidReason) = UsernameHelpers.IsNameValid(requestedUsername);
            if (!nameValid)
            {
                connection.Disconnect($"Username is invalid ({invalidReason}).");
                return;
            }

            var endPoint = connection.RemoteEndPoint;
            var name = requestedUsername;
            var origName = name;
            var iterations = 1;

            while (_assignedSessions.Values.Any(u => u.Username == name))
            {
                // This is shit but I don't care.
                name = $"{origName}_{++iterations}";
            }

            var session = new NetSessionId(name);

            if (OnConnecting(endPoint, session))
            {
                _assignedSessions.Add(connection, session);
                var msg = connection.Peer.CreateMessage();
                msg.Write(name);
                connection.Peer.SendMessage(msg, connection, NetDeliveryMethod.ReliableOrdered);
            }
            else
            {
                connection.Disconnect("Sorry, denied. Why? Couldn't tell you, I didn't implement a deny reason.");
                return;
            }

            NetIncomingMessage okMsg;
            try
            {
                okMsg = await AwaitData(connection);
            }
            catch (ClientDisconnectedException)
            {
                return;
            }

            if (okMsg.ReadString() != "ok")
            {
                connection.Disconnect("You should say ok.");
                return;
            }

            // Handshake complete!
            HandleInitialHandshakeComplete(connection);
        }

        private void HandleInitialHandshakeComplete(NetConnection sender)
        {
            var session = _assignedSessions[sender];

            var channel = new NetChannel(this, sender, session);
            _channels.Add(sender, channel);

            _strings.SendFullTable(channel);

            Logger.InfoS("net", $"{channel.RemoteEndPoint}: Connected");

            OnConnected(channel);
        }

        private void HandleDisconnect(NetConnection connection, string reason)
        {
            var channel = _channels[connection];

            Logger.InfoS("net", $"{channel.RemoteEndPoint}: Disconnected ({reason})");
            _assignedSessions.Remove(connection);

            OnDisconnected(channel);
            _channels.Remove(connection);

            if (IsClient)
            {
                connection.Peer.Shutdown(reason);
                _toCleanNetPeers.Add(connection.Peer);
                _strings.Reset();

                _cancelConnectTokenSource?.Cancel();
                _clientConnectionState = ClientConnectionState.NotConnecting;
            }
        }

        /// <inheritdoc />
        public void DisconnectChannel(INetChannel channel, string reason)
        {
            channel.Disconnect(reason);
        }

        private bool DispatchNetMessage(NetIncomingMessage msg)
        {
            var peer = msg.SenderConnection.Peer;
            if (peer.Status == NetPeerStatus.ShutdownRequested)
                return true;

            if (peer.Status == NetPeerStatus.NotRunning)
                return true;

            if (!IsConnected)
                return true;

            if (_awaitingData.TryGetValue(msg.SenderConnection, out var info))
            {
                var (cancel, tcs) = info;
                _awaitingData.Remove(msg.SenderConnection);
                cancel.Dispose();
                tcs.TrySetResult(msg);
                return false;
            }

            if (msg.LengthBytes < 1)
            {
                Logger.WarningS("net", $"{msg.SenderConnection.RemoteEndPoint}: Received empty packet.");
                return true;
            }

            var id = msg.ReadByte();

            if (_netMsgFunctions[id].Item1 == null)
            {
                if (!CacheNetMsgFunction(id))
                {
                    Logger.WarningS("net", $"{msg.SenderConnection.RemoteEndPoint}: Got net message with invalid ID {id}.");
                    return true;
                }
            }

            var (func, type) = _netMsgFunctions[id];

            var channel = GetChannel(msg.SenderConnection);
            var instance = func(channel);
            instance.MsgChannel = channel;

            try
            {
                instance.ReadFromBuffer(msg);
            }
            catch (InvalidCastException ice)
            {
                Logger.ErrorS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Wrong deserialization of {type.Name} packet: {ice.Message}");
                throw;
            }
            catch (Exception e) // yes, we want to catch ALL exeptions for security
            {
                Logger.WarningS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Failed to deserialize {type.Name} packet: {e.Message}");
            }

            if (!_callbacks.TryGetValue(type, out ProcessMessage callback))
            {
                Logger.WarningS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Received packet {id}:{type}, but callback was not registered.");
                return true;
            }

            callback?.Invoke(instance);
            return true;
        }

        private bool CacheNetMsgFunction(byte id)
        {
            if (!_strings.TryGetString(id, out var name))
            {
                return false;
            }

            if (!_messages.TryGetValue(name, out var packetType))
            {
                return false;
            }

            var constructor = packetType.GetConstructor(new[] {typeof(INetChannel)});

            DebugTools.AssertNotNull(constructor);

            var dynamicMethod = new DynamicMethod($"_netMsg<>{id}", typeof(NetMessage), new[]{typeof(INetChannel)}, packetType, false);

            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "channel");

            var gen = dynamicMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Newobj, constructor);
            gen.Emit(OpCodes.Ret);

            var @delegate =
                (Func<INetChannel, NetMessage>) dynamicMethod.CreateDelegate(typeof(Func<INetChannel, NetMessage>));

            _netMsgFunctions[id] = (@delegate, packetType);
            return true;
        }

        #region NetMessages

        /// <inheritdoc />
        public void RegisterNetMessage<T>(string name, ProcessMessage<T> rxCallback = null)
            where T : NetMessage
        {
            _strings.AddString(name);

            _messages.Add(name, typeof(T));

            if (rxCallback != null)
                _callbacks.Add(typeof(T), msg => rxCallback((T) msg));
        }

        /// <inheritdoc />
        public T CreateNetMessage<T>()
            where T : NetMessage
        {
            if (!_blankNetMsgFunctions.TryGetValue(typeof(T), out var func))
            {
                CacheBlankFunction(typeof(T));
                func = _blankNetMsgFunctions[typeof(T)];
            }
            return (T) func();
        }

        private void CacheBlankFunction(Type type)
        {
            var constructor = type.GetConstructor(new[] {typeof(INetChannel)});

            DebugTools.AssertNotNull(constructor);

            var dynamicMethod = new DynamicMethod($"_netMsg<>{type.Name}", typeof(NetMessage), Array.Empty<Type>(), type, false);
            var gen = dynamicMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Newobj, constructor);
            gen.Emit(OpCodes.Ret);

            var @delegate = (Func<NetMessage>) dynamicMethod.CreateDelegate(typeof(Func<NetMessage>));

            _blankNetMsgFunctions.Add(type, @delegate);
        }

        private NetOutgoingMessage BuildMessage(NetMessage message, NetPeer peer)
        {
            var packet = peer.CreateMessage(4);

            if (!_strings.TryFindStringId(message.MsgName, out int msgId))
                throw new NetManagerException(
                    $"[NET] No string in table with name {message.MsgName}. Was it registered?");

            packet.Write((byte) msgId);
            message.WriteToBuffer(packet);
            return packet;
        }

        /// <inheritdoc />
        public void ServerSendToAll(NetMessage message)
        {
            DebugTools.Assert(IsServer);

            if (!IsConnected)
                return;

            foreach (var peer in _netPeers)
            {
                var packet = BuildMessage(message, peer);
                var method = message.DeliveryMethod;
                if (peer.ConnectionsCount == 0)
                {
                    continue;
                }

                peer.SendMessage(packet, peer.Connections, method, 0);
            }
        }

        /// <inheritdoc />
        public void ServerSendMessage(NetMessage message, INetChannel recipient)
        {
            DebugTools.Assert(IsServer);
            if (!(recipient is NetChannel channel))
                throw new ArgumentException($"Not of type {typeof(NetChannel).FullName}", nameof(recipient));

            var peer = channel.Connection.Peer;
            var packet = BuildMessage(message, peer);
            var method = message.DeliveryMethod;
            peer.SendMessage(packet, channel.Connection, method);
        }

        /// <inheritdoc />
        public void ServerSendToMany(NetMessage message, List<INetChannel> recipients)
        {
            DebugTools.Assert(IsServer);
            if (!IsConnected)
                return;

            foreach (var channel in recipients)
            {
                ServerSendMessage(message, channel);
            }
        }

        /// <inheritdoc />
        public void ClientSendMessage(NetMessage message)
        {
            DebugTools.Assert(IsClient);

            // not connected to a server, so a message cannot be sent to it.
            if (!IsConnected)
                return;

            DebugTools.Assert(_netPeers.Count == 1);
            DebugTools.Assert(_netPeers[0].ConnectionsCount == 1);

            var peer = _netPeers[0];
            var packet = BuildMessage(message, peer);
            var method = message.DeliveryMethod;
            peer.SendMessage(packet, peer.Connections[0], method);
        }

        #endregion NetMessages

        #region Events

        protected virtual bool OnConnecting(IPEndPoint ip, NetSessionId sessionId)
        {
            var args = new NetConnectingArgs(sessionId, ip);
            Connecting?.Invoke(this, args);
            return !args.Deny;
        }

        protected virtual void OnConnectFailed(string reason)
        {
            var args = new NetConnectFailArgs(reason);
            ConnectFailed?.Invoke(this, args);
        }

        protected virtual void OnConnected(INetChannel channel)
        {
            Connected?.Invoke(this, new NetChannelArgs(channel));
        }

        protected virtual void OnDisconnected(INetChannel channel)
        {
            Disconnect?.Invoke(this, new NetChannelArgs(channel));
        }

        /// <inheritdoc />
        public event EventHandler<NetConnectingArgs> Connecting;

        /// <inheritdoc />
        public event EventHandler<NetConnectFailArgs> ConnectFailed;

        /// <inheritdoc />
        public event EventHandler<NetChannelArgs> Connected;

        /// <inheritdoc />
        public event EventHandler<NetChannelArgs> Disconnect;

        #endregion Events

        private enum ClientConnectionState
        {
            /// <summary>
            ///     We are not connected and not trying to get connected either. Quite lonely huh.
            /// </summary>
            NotConnecting,

            /// <summary>
            ///     Resolving the DNS query for the address of the server.
            /// </summary>
            ResolvingHost,

            /// <summary>
            ///     Attempting to establish a connection to the server.
            /// </summary>
            EstablishingConnection,

            /// <summary>
            ///     Connection established, going through regular handshake business.
            /// </summary>
            Handshake,

            /// <summary>
            ///     Connection is solid and handshake is done go wild.
            /// </summary>
            Connected
        }

        [Serializable]
        public class ClientDisconnectedException : Exception
        {
            public ClientDisconnectedException()
            {
            }

            public ClientDisconnectedException(string message) : base(message)
            {
            }

            public ClientDisconnectedException(string message, Exception inner) : base(message, inner)
            {
            }

            protected ClientDisconnectedException(
                SerializationInfo info,
                StreamingContext context) : base(info, context)
            {
            }
        }
    }

    /// <summary>
    ///     Generic exception thrown by the NetManager class.
    /// </summary>
    public class NetManagerException : Exception
    {
        public NetManagerException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    ///     Traffic statistics for a NetChannel.
    /// </summary>
    public struct NetworkStats
    {
        /// <summary>
        ///     Total sent bytes.
        /// </summary>
        public readonly int SentBytes;

        /// <summary>
        ///     Total received bytes.
        /// </summary>
        public readonly int ReceivedBytes;

        /// <summary>
        ///     Total sent packets.
        /// </summary>
        public readonly int SentPackets;

        /// <summary>
        ///     Total received packets.
        /// </summary>
        public readonly int ReceivedPackets;

        public NetworkStats(int sentBytes, int receivedBytes, int sentPackets, int receivedPackets)
        {
            SentBytes = sentBytes;
            ReceivedBytes = receivedBytes;
            SentPackets = sentPackets;
            ReceivedPackets = receivedPackets;
        }

        /// <summary>
        ///     Creates an instance of this object.
        /// </summary>
        public NetworkStats(NetPeerStatistics statistics)
        {
            SentBytes = statistics.SentBytes;
            ReceivedBytes = statistics.ReceivedBytes;
            SentPackets = statistics.SentPackets;
            ReceivedPackets = statistics.ReceivedPackets;
        }
    }
}
