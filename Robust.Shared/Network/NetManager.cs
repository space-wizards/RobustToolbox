using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

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
    public partial class NetManager : IClientNetManager, IServerNetManager
    {
        internal const int AesKeyLength = 32;

        [Dependency] private readonly IRobustSerializer _serializer = default!;

        private static readonly Counter SentPacketsMetrics = Metrics.CreateCounter(
            "robust_net_sent_packets",
            "Number of packets sent since server startup.");

        private static readonly Counter RecvPacketsMetrics = Metrics.CreateCounter(
            "robust_net_recv_packets",
            "Number of packets received since server startup.");

        private static readonly Counter SentMessagesMetrics = Metrics.CreateCounter(
            "robust_net_sent_messages",
            "Number of messages sent since server startup.");

        private static readonly Counter RecvMessagesMetrics = Metrics.CreateCounter(
            "robust_net_recv_messages",
            "Number of messages received since server startup.");

        private static readonly Counter SentBytesMetrics = Metrics.CreateCounter(
            "robust_net_sent_bytes",
            "Number of bytes sent since server startup.");

        private static readonly Counter RecvBytesMetrics = Metrics.CreateCounter(
            "robust_net_recv_bytes",
            "Number of bytes received since server startup.");

        private static readonly Counter MessagesResentDelayMetrics = Metrics.CreateCounter(
            "robust_net_resent_delay",
            "Number of messages that had to be re-sent due to delay.");

        private static readonly Counter MessagesResentHoleMetrics = Metrics.CreateCounter(
            "robust_net_resent_hole",
            "Number of messages that had to be re-sent due to holes.");

        private static readonly Counter MessagesDroppedMetrics = Metrics.CreateCounter(
            "robust_net_dropped",
            "Number of incoming messages that have been dropped.");

        // TODO: Disabled for now since calculating these from Lidgren is way too expensive.
        // Need to go through and have Lidgren properly keep track of counters for these.
        /*
        private static readonly Gauge MessagesStoredMetrics = Metrics.CreateGauge(
            "robust_net_stored",
            "Number of stored messages for reliable resending (if necessary).");

        private static readonly Gauge MessagesUnsentMetrics = Metrics.CreateGauge(
            "robust_net_unsent",
            "Number of queued (unsent) messages that have yet to be sent.");
        */

        private readonly Dictionary<Type, ProcessMessage> _callbacks = new();

        /// <summary>
        ///     Holds the synced lookup table of NetConnection -> NetChannel
        /// </summary>
        private readonly Dictionary<NetConnection, NetChannel> _channels = new();

        private readonly Dictionary<string, NetConnection> _assignedUsernames = new();

        private readonly Dictionary<NetUserId, NetConnection> _assignedUserIds =
            new();

        // Used for processing incoming net messages.
        private readonly NetMsgEntry[] _netMsgFunctions = new NetMsgEntry[256];

        // Used for processing outgoing net messages.
        private readonly Dictionary<Type, Func<NetMessage>> _blankNetMsgFunctions =
            new();

        private readonly Dictionary<Type, long> _bandwidthUsage = new();

        [Dependency] private readonly IConfigurationManagerInternal _config = default!;
        [Dependency] private readonly IAuthManager _authManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        /// <summary>
        ///     Holds lookup table for NetMessage.Id -> NetMessage.Type
        /// </summary>
        private readonly Dictionary<string, (Type type, bool isHandshake)> _messages = new();

        /// <summary>
        /// The StringTable for transforming packet Ids to Packet name.
        /// </summary>
        private readonly StringTable _strings;

        /// <summary>
        ///     The list of network peers we are listening on.
        /// </summary>
        private readonly List<NetPeerData> _netPeers = new();

        // Client connect happens during status changed and such callbacks, so we need to defer deletion of these.
        private readonly List<NetPeer> _toCleanNetPeers = new();

        private readonly Dictionary<NetConnection, TaskCompletionSource<object?>> _awaitingDisconnect
            = new();

        private readonly HashSet<NetUserId> _awaitingDisconnectToConnect = new HashSet<NetUserId>();

        /// <inheritdoc />
        public int Port => _config.GetCVar(CVars.NetPort);

        public bool IsAuthEnabled => _config.GetCVar<bool>("auth.enabled");

        public IReadOnlyDictionary<Type, long> MessageBandwidthUsage => _bandwidthUsage;

        private NetEncryption? _clientEncryption;

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
                    if (p.Peer.ConnectionsCount > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsRunning => _netPeers.Count != 0;

        public NetworkStats Statistics
        {
            get
            {
                var sentPackets = 0L;
                var sentBytes = 0L;
                var recvPackets = 0L;
                var recvBytes = 0L;

                foreach (var peer in _netPeers)
                {
                    var netPeerStatistics = peer.Peer.Statistics;
                    sentPackets += netPeerStatistics.SentPackets;
                    sentBytes += netPeerStatistics.SentBytes;
                    recvPackets += netPeerStatistics.ReceivedPackets;
                    recvBytes += netPeerStatistics.ReceivedBytes;
                }

                return new NetworkStats(sentBytes, recvBytes, sentPackets, recvPackets);
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public IEnumerable<INetChannel> Channels => _channels.Values;

        /// <inheritdoc />
        public int ChannelCount => _channels.Count;

        public IReadOnlyDictionary<Type, ProcessMessage> CallbackAudit => _callbacks;

        /// <inheritdoc />
        public INetChannel? ServerChannel => ServerChannelImpl;

        private NetChannel? ServerChannelImpl
        {
            get
            {
                DebugTools.Assert(IsClient);

                if (_netPeers.Count == 0)
                {
                    return null;
                }

                var peer = _netPeers[0];
                return peer.Channels.Count == 0 ? null : peer.Channels[0];
            }
        }

        private bool _initialized;

        public NetManager()
        {
            _strings = new StringTable(this);
        }

        public void ResetBandwidthMetrics()
        {
            _bandwidthUsage.Clear();
        }

        /// <inheritdoc />
        public void Initialize(bool isServer)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("NetManager has already been initialized.");
            }

            SynchronizeNetTime();

            IsServer = isServer;

            _config.OnValueChanged(CVars.NetVerbose, NetVerboseChanged);
            if (isServer)
            {
                _config.OnValueChanged(CVars.AuthMode, OnAuthModeChanged, invokeImmediately: true);
            }
#if DEBUG
            _config.OnValueChanged(CVars.NetFakeLoss, _fakeLossChanged);
            _config.OnValueChanged(CVars.NetFakeLagMin, _fakeLagMinChanged);
            _config.OnValueChanged(CVars.NetFakeLagRand, _fakeLagRandomChanged);
            _config.OnValueChanged(CVars.NetFakeDuplicates, FakeDuplicatesChanged);
#endif

            _strings.Initialize(() => { Logger.InfoS("net", "Message string table loaded."); },
                UpdateNetMessageFunctions);
            _serializer.ClientHandshakeComplete += OnSerializerOnClientHandshakeComplete;

            _initialized = true;

            if (IsServer)
            {
                SAGenerateRsaKeys();
            }
        }

        private void OnAuthModeChanged(int mode)
        {
            Auth = (AuthMode)mode;
        }

        private void OnSerializerOnClientHandshakeComplete()
        {
            Logger.InfoS("net", "Client completed serializer handshake.");
            OnConnected(ServerChannelImpl!);
        }

        private void SynchronizeNetTime()
        {
            // Synchronize Lidgren NetTime with our RealTime.

            for (var i = 0; i < 10; i++)
            {
                // Try and set this in a loop to avoid any JIT hang fuckery or similar.
                // Loop until the time is within acceptable margin.
                // Fixing this "properly" would basically require re-architecturing Lidgren to do DI stuff
                // so we can more sanely wire these together.
                NetTime.SetNow(_timing.RealTime.TotalSeconds);
                var dev = TimeSpan.FromSeconds(NetTime.Now) - _timing.RealTime;

                if (Math.Abs(dev.TotalMilliseconds) < 0.05)
                    break;
            }
        }

        private void UpdateNetMessageFunctions(MsgStringTableEntries.Entry[] entries)
        {
            foreach (var entry in entries)
            {
                if (entry.Id > byte.MaxValue)
                {
                    continue;
                }

                CacheNetMsgFunction((byte) entry.Id);
            }
        }

        private void NetVerboseChanged(bool on)
        {
            foreach (var peer in _netPeers)
            {
                peer.Peer.Configuration.SetMessageTypeEnabled(NetIncomingMessageType.VerboseDebugMessage, on);
            }
        }

        public void StartServer()
        {
            DebugTools.Assert(IsServer);
            DebugTools.Assert(!IsRunning);

            var binds = _config.GetCVar(CVars.NetBindTo).Split(',');
            var dualStack = _config.GetCVar(CVars.NetDualStack);

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
                // Disabled for now since we aren't doing anything with the connection approval stuff.
                // config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

                if (address.AddressFamily == AddressFamily.InterNetworkV6 && dualStack)
                {
                    foundIpv6 = true;
                    config.DualStack = true;
                }

                var peer = IsServer ? (NetPeer) new NetServer(config) : new NetClient(config);
                peer.Start();
                _netPeers.Add(new NetPeerData(peer));
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

        /// <inheritdoc />
        public void Shutdown(string reason)
        {
            foreach (var kvChannel in _channels)
                DisconnectChannel(kvChannel.Value, reason);

            // request shutdown of the netPeer
            _netPeers.ForEach(p => p.Peer.Shutdown(reason));

            // wait for the network thread to finish its work (like flushing packets and gracefully disconnecting)
            // Lidgren does not expose the thread, so we can't join or or anything
            // pretty much have to poll every so often and wait for it to finish before continuing
            // when the network thread is finished, it will change status from ShutdownRequested to NotRunning
            while (_netPeers.Any(p => p.Peer.Status == NetPeerStatus.ShutdownRequested))
            {
                // sleep the thread for an arbitrary length so it isn't spinning in the while loop as much
                Thread.Sleep(50);
            }

            _netPeers.Clear();

            // Clear cached message functions.
            Array.Clear(_netMsgFunctions, 0, _netMsgFunctions.Length);
            _blankNetMsgFunctions.Clear();
            // Clear string table.
            // This has to be done AFTER clearing _netMsgFunctions so that it re-initializes NetMsg 0.
            _strings.Reset();
            _messages.Clear();

            _config.UnsubValueChanged(CVars.NetVerbose, NetVerboseChanged);
            if (IsServer)
            {
                _config.UnsubValueChanged(CVars.AuthMode, OnAuthModeChanged);
            }
#if DEBUG
            _config.UnsubValueChanged(CVars.NetFakeLoss, _fakeLossChanged);
            _config.UnsubValueChanged(CVars.NetFakeLagMin, _fakeLagMinChanged);
            _config.UnsubValueChanged(CVars.NetFakeLagRand, _fakeLagRandomChanged);
            _config.UnsubValueChanged(CVars.NetFakeDuplicates, FakeDuplicatesChanged);
#endif

            _serializer.ClientHandshakeComplete -= OnSerializerOnClientHandshakeComplete;

            _cancelConnectTokenSource?.Cancel();
            ClientConnectState = ClientConnectionState.NotConnecting;

            ConnectFailed = null;
            Connected = null;
            Disconnect = null;
            _connectingEvent.Clear();


            _initialized = false;
        }

        public void ProcessPackets()
        {
            var sentMessages = 0L;
            var recvMessages = 0L;
            var sentBytes = 0L;
            var recvBytes = 0L;
            var sentPackets = 0L;
            var recvPackets = 0L;
            var resentDelays = 0L;
            var resentHoles = 0L;
            var dropped = 0L;
            /*
            var unsent = 0L;
            var stored = 0L;
            */

            foreach (var peer in _netPeers)
            {
                NetIncomingMessage msg;
                var recycle = true;
                while ((msg = peer.Peer.ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                            Logger.DebugS("net", "{PeerAddress}: {Message}", peer.Peer.Configuration.LocalAddress,
                                msg.ReadString());
                            break;

                        case NetIncomingMessageType.DebugMessage:
                            Logger.InfoS("net", "{PeerAddress}: {Message}", peer.Peer.Configuration.LocalAddress,
                                msg.ReadString());
                            break;

                        case NetIncomingMessageType.WarningMessage:
                            Logger.WarningS("net", "{PeerAddress}: {Message}", peer.Peer.Configuration.LocalAddress,
                                msg.ReadString());
                            break;

                        case NetIncomingMessageType.ErrorMessage:
                            Logger.ErrorS("net", "{PeerAddress}: {Message}", peer.Peer.Configuration.LocalAddress,
                                msg.ReadString());
                            break;

                        case NetIncomingMessageType.ConnectionApproval:
                            HandleApproval(msg);
                            recycle = false;
                            break;

                        case NetIncomingMessageType.Data:
                            recycle = DispatchNetMessage(msg);
                            break;

                        case NetIncomingMessageType.StatusChanged:
                            HandleStatusChanged(peer, msg);
                            break;

                        default:
                            Logger.WarningS("net",
                                "{0}: Unhandled incoming packet type from {1}: {2}",
                                peer.Peer.Configuration.LocalAddress,
                                msg.SenderConnection.RemoteEndPoint,
                                msg.MessageType);
                            break;
                    }

                    if (recycle)
                    {
                        peer.Peer.Recycle(msg);
                    }
                }

                var statistics = peer.Peer.Statistics;
                sentMessages += statistics.SentMessages;
                recvMessages += statistics.ReceivedMessages;
                sentBytes += statistics.SentBytes;
                recvBytes += statistics.ReceivedBytes;
                sentPackets += statistics.SentPackets;
                recvPackets += statistics.ReceivedPackets;
                resentDelays += statistics.ResentMessagesDueToDelay;
                resentHoles += statistics.ResentMessagesDueToHole;
                dropped += statistics.DroppedMessages;

                /*
                statistics.CalculateUnsentAndStoredMessages(out var pUnsent, out var pStored);
                unsent += pUnsent;
                stored += pStored;
                */
            }

            if (_toCleanNetPeers.Count != 0)
            {
                foreach (var peer in _toCleanNetPeers)
                {
                    _netPeers.RemoveAll(p => p.Peer == peer);
                }

                _toCleanNetPeers.Clear();
            }

            SentMessagesMetrics.IncTo(sentMessages);
            RecvMessagesMetrics.IncTo(recvMessages);
            SentBytesMetrics.IncTo(sentBytes);
            RecvBytesMetrics.IncTo(recvBytes);
            SentPacketsMetrics.IncTo(sentPackets);
            RecvPacketsMetrics.IncTo(recvPackets);
            MessagesResentDelayMetrics.IncTo(resentDelays);
            MessagesResentHoleMetrics.IncTo(resentHoles);
            MessagesDroppedMetrics.IncTo(dropped);

            /*
            MessagesUnsentMetrics.Set(unsent);
            MessagesStoredMetrics.Set(stored);
            */
        }

        /// <inheritdoc />
        public void ClientDisconnect(string reason)
        {
            DebugTools.Assert(IsClient, "Should never be called on the server.");
            if (ServerChannel != null)
            {
                Disconnect?.Invoke(this, new NetDisconnectedArgs(ServerChannel, reason));
            }

            Shutdown(reason);
        }

        private NetPeerConfiguration _getBaseNetPeerConfig()
        {
            var netConfig = new NetPeerConfiguration("SS14_NetTag");

            // ping the client once per second.
            netConfig.PingInterval = 1f;

            netConfig.SendBufferSize = _config.GetCVar(CVars.NetSendBufferSize);
            netConfig.ReceiveBufferSize = _config.GetCVar(CVars.NetReceiveBufferSize);
            netConfig.MaximumHandshakeAttempts = 5;

            var verbose = _config.GetCVar(CVars.NetVerbose);
            netConfig.SetMessageTypeEnabled(NetIncomingMessageType.VerboseDebugMessage, verbose);

            if (IsServer)
            {
                netConfig.SetMessageTypeEnabled(NetIncomingMessageType.ConnectionApproval, true);
                netConfig.MaximumConnections = _config.GetCVar(CVars.GameMaxPlayers);
            }
            else
            {
                netConfig.ConnectionTimeout = _config.GetCVar(CVars.ConnectionTimeout);
                netConfig.ResendHandshakeInterval = _config.GetCVar(CVars.ResendHandshakeInterval);
                netConfig.MaximumHandshakeAttempts = _config.GetCVar(CVars.MaximumHandshakeAttempts);
            }


#if DEBUG
            //Simulate Latency
            netConfig.SimulatedLoss = _config.GetCVar(CVars.NetFakeLoss);
            netConfig.SimulatedMinimumLatency = _config.GetCVar(CVars.NetFakeLagMin);
            netConfig.SimulatedRandomLatency = _config.GetCVar(CVars.NetFakeLagRand);
            netConfig.SimulatedDuplicatesChance = _config.GetCVar(CVars.NetFakeDuplicates);

            netConfig.ConnectionTimeout = 30000f;
#endif
            return netConfig;
        }

#if DEBUG
        private void _fakeLossChanged(float newValue)
        {
            foreach (var peer in _netPeers)
            {
                peer.Peer.Configuration.SimulatedLoss = newValue;
            }
        }

        private void _fakeLagMinChanged(float newValue)
        {
            foreach (var peer in _netPeers)
            {
                peer.Peer.Configuration.SimulatedMinimumLatency = newValue;
            }
        }

        private void _fakeLagRandomChanged(float newValue)
        {
            foreach (var peer in _netPeers)
            {
                peer.Peer.Configuration.SimulatedRandomLatency = newValue;
            }
        }

        private void FakeDuplicatesChanged(float newValue)
        {
            foreach (var peer in _netPeers)
            {
                peer.Peer.Configuration.SimulatedDuplicatesChance = newValue;
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

            if (_channels.TryGetValue(connection, out var channel))
                return channel;

            throw new NetManagerException("There is no NetChannel for this NetConnection.");
        }

        private bool TryGetChannel(NetConnection connection, [NotNullWhen(true)] out INetChannel? channel)
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

        private void HandleStatusChanged(NetPeerData peer, NetIncomingMessage msg)
        {
            var sender = msg.SenderConnection;
            msg.ReadByte();
            var reason = msg.ReadString();
            Logger.DebugS("net",
                "{ConnectionEndpoint}: Status changed to {ConnectionStatus}, reason: {ConnectionStatusReason}",
                sender.RemoteEndPoint, sender.Status, reason);

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
                        HandleHandshake(peer, sender);
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
                        HandleDisconnect(peer, sender, reason);
                    }

                    if (_awaitingDisconnect.TryGetValue(sender, out var tcs))
                    {
                        tcs.TrySetResult(null);
                    }

                    break;
            }
        }

        private async void HandleInitialHandshakeComplete(NetPeerData peer,
            NetConnection sender,
            NetUserData userData,
            NetEncryption? encryption,
            LoginType loginType)
        {
            var channel = new NetChannel(this, sender, userData, loginType);
            _assignedUserIds.Add(userData.UserId, sender);
            _assignedUsernames.Add(userData.UserName, sender);
            _channels.Add(sender, channel);
            peer.AddChannel(channel);
            channel.Encryption = encryption;

            _strings.SendFullTable(channel);

            try
            {
                await _serializer.Handshake(channel);
            }
            catch (TaskCanceledException)
            {
                // Client disconnected during handshake.
                return;
            }

            Logger.InfoS("net", "{ConnectionEndpoint}: Connected", channel.RemoteEndPoint);

            OnConnected(channel);
        }

        private void HandleDisconnect(NetPeerData peer, NetConnection connection, string reason)
        {
            var channel = _channels[connection];

            Logger.InfoS("net", "{ConnectionEndpoint}: Disconnected ({DisconnectReason})", channel.RemoteEndPoint,
                reason);
            _assignedUsernames.Remove(channel.UserName);
            _assignedUserIds.Remove(channel.UserId);

#if EXCEPTION_TOLERANCE
            try
            {
#endif
                OnDisconnected(channel, reason);
#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                // A throw aborting in the middle of this method would be *really* bad
                // and cause fun bugs like ghost clients sticking around.
                // I say "would" as if it hasn't already happened...
                Logger.ErrorS("net", "Caught exception in OnDisconnected handler:\n{0}", e);
            }
#endif
            _channels.Remove(connection);
            peer.RemoveChannel(channel);

            if (IsClient)
            {
                connection.Peer.Shutdown(reason);
                _toCleanNetPeers.Add(connection.Peer);
                _strings.Reset();

                _cancelConnectTokenSource?.Cancel();
                ClientConnectState = ClientConnectionState.NotConnecting;
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
            {
                Logger.WarningS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Received data message, but shutdown is requested.");
                return true;
            }

            if (peer.Status == NetPeerStatus.NotRunning)
            {
                Logger.WarningS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Received data message, peer is not running.");
                return true;
            }

            if (!IsConnected)
            {
                Logger.WarningS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Received data message, but not connected.");
                return true;
            }

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

            var channel = _channels[msg.SenderConnection];

            var encryption = IsServer ? channel.Encryption : _clientEncryption;

            if (encryption != null)
            {
                msg.Decrypt(encryption);
            }

            var id = msg.ReadByte();

            ref var entry = ref _netMsgFunctions[id];

            if (entry.CreateFunction == null)
            {
                Logger.WarningS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Got net message with invalid ID {id}.");

                channel.Disconnect("Got NetMessage with invalid ID");
                return true;
            }

            if (!channel.IsHandshakeComplete && !entry.IsHandshake)
            {
                Logger.WarningS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Got non-handshake message {entry.Type.Name} before handshake completion.");

                channel.Disconnect("Got unacceptable net message before handshake completion");
                return true;
            }

            var type = entry.Type;

            var instance = entry.CreateFunction(channel);
            instance.MsgChannel = channel;

#if DEBUG

            if (!_bandwidthUsage.TryGetValue(type, out var bandwidth))
            {
                bandwidth = 0;
            }

            _bandwidthUsage[type] = bandwidth + msg.LengthBytes;

#endif

            try
            {
                instance.ReadFromBuffer(msg);
            }
            catch (InvalidCastException ice)
            {
                Logger.ErrorS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Wrong deserialization of {type.Name} packet: {ice.Message}");
                return true;
            }
            catch (Exception e) // yes, we want to catch ALL exeptions for security
            {
                Logger.WarningS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Failed to deserialize {type.Name} packet: {e.Message}");
                return true;
            }

            // Callback must be available or else construction delegate will not be registered.
            var callback = _callbacks[type];

            // Logger.DebugS("net", $"RECV: {instance.GetType().Name}");
            try
            {
                callback?.Invoke(instance);
            }
            catch (Exception e)
            {
                Logger.ErrorS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: exception in message handler for {type.Name}:\n{e}");
            }

            return true;
        }

        private void CacheNetMsgFunction(byte id)
        {
            if (!_strings.TryGetString(id, out var name))
            {
                return;
            }

            if (!_messages.TryGetValue(name, out var msgDat))
            {
                return;
            }

            var (packetType, isHandshake) = msgDat;

            if (!_callbacks.ContainsKey(packetType))
            {
                return;
            }

            var dynamicMethod = new DynamicMethod($"_netMsg<>{name}", typeof(NetMessage), new[] {typeof(INetChannel)},
                packetType, false);

            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "channel");

            var gen = dynamicMethod.GetILGenerator().GetRobustGen();

            // Obsolete path for content
            if (packetType.GetConstructor(new[] {typeof(INetChannel)}) is { } constructor)
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Newobj, constructor);
                gen.Emit(OpCodes.Ret);
            }
            else
            {
                constructor = packetType.GetConstructor(Type.EmptyTypes)!;
                DebugTools.AssertNotNull(constructor);

                gen.DeclareLocal(typeof(NetMessage));

                gen.Emit(OpCodes.Newobj, constructor);
                gen.Emit(OpCodes.Ret);
            }

            var @delegate = dynamicMethod.CreateDelegate<Func<INetChannel, NetMessage>>();

            ref var entry = ref _netMsgFunctions[id];
            entry.CreateFunction = @delegate;
            entry.Type = packetType;
            entry.IsHandshake = isHandshake;
        }

        #region NetMessages

        /// <inheritdoc />
        public void RegisterNetMessage<T>(ProcessMessage<T>? rxCallback = null,
            NetMessageAccept accept = NetMessageAccept.Both)
            where T : NetMessage, new()
        {
            var name = new T().MsgName;
            var id = _strings.AddString(name);

            _messages.Add(name, (typeof(T), (accept & NetMessageAccept.Handshake) != 0));

            var thisSide = IsServer ? NetMessageAccept.Server : NetMessageAccept.Client;

            if (rxCallback != null && (accept & thisSide) != 0)
            {
                _callbacks.Add(typeof(T), msg => rxCallback((T) msg));

                if (id != -1)
                {
                    CacheNetMsgFunction((byte) id);
                }
            }

            // This means we *will* be caching creation delegates for messages that are never sent (by this side).
            // But it means the caching logic isn't behind a TryGetValue in CreateNetMessage<T>,
            // so it no thread safety crap.
            CacheBlankFunction(typeof(T));
        }

        /// <inheritdoc />
        public T CreateNetMessage<T>()
            where T : NetMessage
        {
            return (T) _blankNetMsgFunctions[typeof(T)]();
        }

        private void CacheBlankFunction(Type type)
        {
            var dynamicMethod = new DynamicMethod($"_netMsg<>{type.Name}", typeof(NetMessage), Array.Empty<Type>(),
                type, false);
            var gen = dynamicMethod.GetILGenerator().GetRobustGen();

            // Obsolete path for content
            if (type.GetConstructor(new[] {typeof(INetChannel)}) is { } constructor)
            {
                gen.Emit(OpCodes.Ldnull);
                gen.Emit(OpCodes.Newobj, constructor);
                gen.Emit(OpCodes.Ret);
            }
            else
            {
                constructor = type.GetConstructor(Type.EmptyTypes)!;
                DebugTools.AssertNotNull(constructor);

                gen.Emit(OpCodes.Newobj, constructor);
                gen.Emit(OpCodes.Ret);
            }

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

            foreach (var channel in _channels.Values)
            {
                if (!channel.IsHandshakeComplete)
                    continue;

                ServerSendMessage(message, channel);
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
            if (channel.Encryption != null)
            {
                packet.Encrypt(channel.Encryption);
            }

            var method = message.DeliveryMethod;
            peer.SendMessage(packet, channel.Connection, method);
            LogSend(message, method, packet);
        }

        private static void LogSend(NetMessage message, NetDeliveryMethod method, NetOutgoingMessage packet)
        {
            // Logger.DebugS("net", $"SEND: {message.GetType().Name} {method} {packet.LengthBytes}");
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
            DebugTools.Assert(_netPeers[0].Channels.Count == 1);

            var peer = _netPeers[0];
            var packet = BuildMessage(message, peer.Peer);
            var method = message.DeliveryMethod;
            if (_clientEncryption != null)
            {
                packet.Encrypt(_clientEncryption);
            }

            peer.Peer.SendMessage(packet, peer.ConnectionsWithChannels[0], method);
            LogSend(message, method, packet);
        }

        #endregion NetMessages

        #region Events

        private async Task<NetConnectingArgs> OnConnecting(
            IPEndPoint ip,
            NetUserData userData,
            LoginType loginType)
        {
            var args = new NetConnectingArgs(userData, ip, loginType);
            foreach (var conn in _connectingEvent)
            {
                await conn(args);
            }

            return args;
        }

        private void OnConnectFailed(string reason)
        {
            var args = new NetConnectFailArgs(reason);
            ConnectFailed?.Invoke(this, args);
        }

        private void OnConnected(NetChannel channel)
        {
            channel.IsHandshakeComplete = true;

            Connected?.Invoke(this, new NetChannelArgs(channel));
        }

        private void OnDisconnected(INetChannel channel, string reason)
        {
            Disconnect?.Invoke(this, new NetDisconnectedArgs(channel, reason));
        }

        private readonly List<Func<NetConnectingArgs, Task>> _connectingEvent
            = new();

        /// <inheritdoc />
        public event Func<NetConnectingArgs, Task> Connecting
        {
            add => _connectingEvent.Add(value);
            remove => _connectingEvent.Remove(value);
        }

        /// <inheritdoc />
        public event EventHandler<NetConnectFailArgs>? ConnectFailed;

        /// <inheritdoc />
        public event EventHandler<NetChannelArgs>? Connected;

        /// <inheritdoc />
        public event EventHandler<NetDisconnectedArgs>? Disconnect;

        #endregion Events

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

        private class NetPeerData
        {
            public readonly NetPeer Peer;

            public readonly List<NetChannel> Channels = new();

            // So that we can do ServerSendToAll without a list copy.
            public readonly List<NetConnection> ConnectionsWithChannels = new();

            public NetPeerData(NetPeer peer)
            {
                Peer = peer;
            }

            public void AddChannel(NetChannel channel)
            {
                Channels.Add(channel);
                ConnectionsWithChannels.Add(channel.Connection);
            }

            public void RemoveChannel(NetChannel channel)
            {
                Channels.Remove(channel);
                ConnectionsWithChannels.Remove(channel.Connection);
            }
        }

        private struct NetMsgEntry
        {
            public Func<INetChannel, NetMessage>? CreateFunction;
            public bool IsHandshake;
            public Type Type;
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
        public readonly long SentBytes;

        /// <summary>
        ///     Total received bytes.
        /// </summary>
        public readonly long ReceivedBytes;

        /// <summary>
        ///     Total sent packets.
        /// </summary>
        public readonly long SentPackets;

        /// <summary>
        ///     Total received packets.
        /// </summary>
        public readonly long ReceivedPackets;

        public NetworkStats(long sentBytes, long receivedBytes, long sentPackets, long receivedPackets)
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
