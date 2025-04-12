using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Profiling;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using SpaceWizards.Sodium;

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
    public sealed partial class NetManager : IClientNetManager, IServerNetManager, IPostInjectInit
    {
        internal const int SharedKeyLength = CryptoAeadXChaCha20Poly1305Ietf.KeyBytes; // 32 bytes

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

        /// <summary>
        ///     Holds the synced lookup table of NetConnection -> NetChannel
        /// </summary>
        private readonly Dictionary<NetConnection, NetChannel> _channels = new();

        private readonly Dictionary<string, NetConnection> _assignedUsernames = new();

        private readonly Dictionary<NetUserId, NetConnection> _assignedUserIds =
            new();

        // Used for processing incoming net messages.
        private readonly MessageData?[] _netMsgIndices = new MessageData?[256];

        private readonly Dictionary<Type, long> _bandwidthUsage = new();

        [Dependency] private readonly IRobustSerializer _serializer = default!;
        [Dependency] private readonly IConfigurationManagerInternal _config = default!;
        [Dependency] private readonly IAuthManager _authManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ILogManager _logMan = default!;
        [Dependency] private readonly ProfManager _prof = default!;
        [Dependency] private readonly HttpClientHolder _http = default!;
        [Dependency] private readonly IHWId _hwId = default!;

        /// <summary>
        ///     Holds lookup table for NetMessage.Id -> NetMessage.Type
        /// </summary>
        private readonly Dictionary<string, MessageData> _messages = new();

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

        private ISawmill _logger = default!;
        private ISawmill _loggerPacket = default!;
        private ISawmill _authLogger = default!;

        /// <inheritdoc />
        public int Port => _config.GetCVar(CVars.NetPort);

        public bool IsAuthEnabled => _config.GetCVar<bool>("auth.enabled");

        public IReadOnlyDictionary<Type, long> MessageBandwidthUsage => _bandwidthUsage;

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

        public IReadOnlyDictionary<Type, ProcessMessage> CallbackAudit => _messages
            .Where(e => e.Value.Callback != null)
            .ToDictionary(e => e.Value.Type, e => e.Value.Callback)!;

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

        private int _mainThreadId;

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

            _mainThreadId = Environment.CurrentManagedThreadId;

            _strings.Sawmill = _logger;

            SynchronizeNetTime();

            IsServer = isServer;

            _config.OnValueChanged(CVars.NetLidgrenLogWarning, LidgrenLogWarningChanged);
            _config.OnValueChanged(CVars.NetLidgrenLogError, LidgrenLogErrorChanged);

            _config.OnValueChanged(CVars.NetVerbose, NetVerboseChanged);
            if (isServer)
            {
                _config.OnValueChanged(CVars.AuthMode, OnAuthModeChanged, invokeImmediately: true);
            }

            _config.OnValueChanged(CVars.NetFakeLoss, _fakeLossChanged);
            _config.OnValueChanged(CVars.NetFakeLagMin, _fakeLagMinChanged);
            _config.OnValueChanged(CVars.NetFakeLagRand, _fakeLagRandomChanged);
            _config.OnValueChanged(CVars.NetFakeDuplicates, FakeDuplicatesChanged);

            _strings.Initialize(() => { _logger.Info("Message string table loaded."); },
                UpdateNetMessageFunctions);
            _serializer.ClientHandshakeComplete += OnSerializerOnClientHandshakeComplete;

            _initialized = true;

            if (IsServer)
            {
                SAGenerateKeys();
            }
        }

        private void LidgrenLogWarningChanged(bool newValue)
        {
            foreach (var netPeer in _netPeers)
            {
                netPeer.Peer.Configuration.SetMessageTypeEnabled(NetIncomingMessageType.WarningMessage, newValue);
            }
        }

        private void LidgrenLogErrorChanged(bool newValue)
        {
            foreach (var netPeer in _netPeers)
            {
                netPeer.Peer.Configuration.SetMessageTypeEnabled(NetIncomingMessageType.ErrorMessage, newValue);
            }
        }

        private void OnAuthModeChanged(int mode)
        {
            Auth = (AuthMode)mode;
        }

        private void OnSerializerOnClientHandshakeComplete()
        {
            _logger.Info("Client completed serializer handshake.");
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
                    continue;

                CacheNetMsgIndex(entry.Id, entry.String);
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

            var upnp = _config.GetCVar(CVars.NetUPnP);
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

                if (UpnpCompatible(config) && upnp)
                    config.EnableUPnP = true;

                var peer = IsServer ? (NetPeer) new NetServer(config) : new NetClient(config);
                peer.Start();
                _netPeers.Add(new NetPeerData(peer));
            }

            if (_netPeers.Count == 0)
            {
                _logger.Warning("Exactly 0 addresses have been bound to, nothing will be able to connect to the server.");
            }

            if (!foundIpv6 && dualStack)
            {
                _logger.Warning("IPv6 Dual Stack is enabled but no IPv6 addresses have been bound to. This will not work.");
            }

            if (upnp)
                InitUpnp();
        }

        public void Reset(string reason)
        {
            foreach (var kvChannel in _channels)
            {
                DisconnectChannel(kvChannel.Value, reason);
            }

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
            Array.Clear(_netMsgIndices, 0, _netMsgIndices.Length);

            // Clear string table.
            // This has to be done AFTER clearing _netMsgFunctions so that it re-initializes NetMsg 0.
            _strings.Reset();

            _cancelConnectTokenSource?.Cancel();
            ClientConnectState = ClientConnectionState.NotConnecting;
        }

        /// <inheritdoc />
        public void Shutdown(string reason)
        {
            Reset(reason);

            _messages.Clear();

            _config.UnsubValueChanged(CVars.NetVerbose, NetVerboseChanged);
            if (IsServer)
            {
                _config.UnsubValueChanged(CVars.AuthMode, OnAuthModeChanged);
            }

            _config.UnsubValueChanged(CVars.NetFakeLoss, _fakeLossChanged);
            _config.UnsubValueChanged(CVars.NetFakeLagMin, _fakeLagMinChanged);
            _config.UnsubValueChanged(CVars.NetFakeLagRand, _fakeLagRandomChanged);
            _config.UnsubValueChanged(CVars.NetFakeDuplicates, FakeDuplicatesChanged);
            _config.UnsubValueChanged(CVars.NetLidgrenLogWarning, LidgrenLogWarningChanged);
            _config.UnsubValueChanged(CVars.NetLidgrenLogError, LidgrenLogErrorChanged);

            _serializer.ClientHandshakeComplete -= OnSerializerOnClientHandshakeComplete;

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

            var countProcessed = 0;
            var countDataProcessed = 0;

            foreach (var peer in _netPeers)
            {
                NetIncomingMessage? msg;
                var recycle = true;
                while ((msg = peer.Peer.ReadMessage()) != null)
                {
                    countProcessed += 1;
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                            _logger.Debug("{PeerAddress}: {Message}", peer.Peer.Configuration.LocalAddress,
                                msg.ReadString());
                            break;

                        case NetIncomingMessageType.DebugMessage:
                            _logger.Info("{PeerAddress}: {Message}", peer.Peer.Configuration.LocalAddress,
                                msg.ReadString());
                            break;

                        case NetIncomingMessageType.WarningMessage:
                            _logger.Warning("{PeerAddress}: {Message}", peer.Peer.Configuration.LocalAddress,
                                msg.ReadString());
                            break;

                        case NetIncomingMessageType.ErrorMessage:
                            _logger.Error("{PeerAddress}: {Message}", peer.Peer.Configuration.LocalAddress,
                                msg.ReadString());
                            break;

                        case NetIncomingMessageType.ConnectionApproval:
                            HandleApproval(msg);
                            recycle = false;
                            break;

                        case NetIncomingMessageType.Data:
                            countDataProcessed += 1;
                            recycle = DispatchNetMessage(msg);
                            break;

                        case NetIncomingMessageType.StatusChanged:
                            HandleStatusChanged(peer, msg);
                            break;

                        default:
                            _logger.Warning("{0}: Unhandled incoming packet type from {1}: {2}",
                                peer.Peer.Configuration.LocalAddress,
                                msg.SenderConnection?.RemoteEndPoint,
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

            _prof.WriteValue("Count Processed", countProcessed);
            _prof.WriteValue("Count Data Processed", countDataProcessed);

            /*
            MessagesUnsentMetrics.Set(unsent);
            MessagesStoredMetrics.Set(stored);
            */
        }

        /// <inheritdoc />
        public void ClientDisconnect(string reason)
        {
            DebugTools.Assert(IsClient, "Should never be called on the server.");

            // First handle any in-progress connection attempt
            if (ClientConnectState != ClientConnectionState.NotConnecting)
            {
                _cancelConnectTokenSource?.Cancel();
            }

            // Then handle existing connection if any
            if (ServerChannel != null)
            {
                Disconnect?.Invoke(this, new NetDisconnectedArgs(ServerChannel, reason));
            }

            Reset(reason);
        }

        private NetPeerConfiguration _getBaseNetPeerConfig()
        {
            var netConfig = new NetPeerConfiguration(_config.GetCVar(CVars.NetLidgrenAppIdentifier));

            // ping the client once per second.
            netConfig.PingInterval = 1f;

            netConfig.SetMessageTypeEnabled(
                NetIncomingMessageType.WarningMessage,
                _config.GetCVar(CVars.NetLidgrenLogWarning));

            netConfig.SetMessageTypeEnabled(
                NetIncomingMessageType.ErrorMessage,
                _config.GetCVar(CVars.NetLidgrenLogError));

            var poolSize = _config.GetCVar(CVars.NetPoolSize);

            if (poolSize <= 0)
            {
                netConfig.UseMessageRecycling = false;
            }
            else
            {
                netConfig.RecycledCacheMaxCount = Math.Min(poolSize, 8192);
            }

            netConfig.SendBufferSize = _config.GetCVar(CVars.NetSendBufferSize);
            netConfig.ReceiveBufferSize = _config.GetCVar(CVars.NetReceiveBufferSize);
            netConfig.MaximumHandshakeAttempts = 5;

            var verbose = _config.GetCVar(CVars.NetVerbose);
            netConfig.SetMessageTypeEnabled(NetIncomingMessageType.VerboseDebugMessage, verbose);

            if (IsServer)
            {
                netConfig.SetMessageTypeEnabled(NetIncomingMessageType.ConnectionApproval, true);
                netConfig.MaximumConnections = _config.GetEffectiveMaxConnections();
            }
            else
            {
                netConfig.ConnectionTimeout = _config.GetCVar(CVars.ConnectionTimeout);
                netConfig.ResendHandshakeInterval = _config.GetCVar(CVars.ResendHandshakeInterval);
                netConfig.MaximumHandshakeAttempts = _config.GetCVar(CVars.MaximumHandshakeAttempts);
            }


            //Simulate Latency
            netConfig.SimulatedLoss = _config.GetCVar(CVars.NetFakeLoss);
            netConfig.SimulatedMinimumLatency = _config.GetCVar(CVars.NetFakeLagMin);
            netConfig.SimulatedRandomLatency = _config.GetCVar(CVars.NetFakeLagRand);
            netConfig.SimulatedDuplicatesChance = _config.GetCVar(CVars.NetFakeDuplicates);

#if DEBUG
            netConfig.ConnectionTimeout = 30000f;
#endif

            // MTU stuff.
            netConfig.MaximumTransmissionUnit = _config.GetCVar(CVars.NetMtu);
            netConfig.MaximumTransmissionUnitV6 = _config.GetCVar(CVars.NetMtuIpv6);
            netConfig.AutoExpandMTU = _config.GetCVar(CVars.NetMtuExpand);
            netConfig.ExpandMTUFrequency = _config.GetCVar(CVars.NetMtuExpandFrequency);
            netConfig.ExpandMTUFailAttempts = _config.GetCVar(CVars.NetMtuExpandFailAttempts);

            return netConfig;
        }

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
            DebugTools.Assert(sender != null);

            var newStatus = (NetConnectionStatus) msg.ReadByte();
            var reason = msg.ReadString();
            _logger.Debug("{ConnectionEndpoint}: Status changed to {ConnectionStatus}, reason: {ConnectionStatusReason}",
                sender.RemoteEndPoint, newStatus, reason);

            if (_awaitingStatusChange.TryGetValue(sender, out var resume))
            {
                _awaitingStatusChange.Remove(sender);
                resume.Item1.Dispose();
                resume.Item2.SetResult(reason);
                return;
            }

            switch (newStatus)
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
            _logger.Verbose($"{sender.RemoteEndPoint}: Initial handshake complete!");

            var channel = new NetChannel(this, sender, userData, loginType);
            _assignedUserIds.Add(userData.UserId, sender);
            _assignedUsernames.Add(userData.UserName, sender);
            _channels.Add(sender, channel);
            peer.AddChannel(channel);
            channel.Encryption = encryption;
            SetupEncryptionChannel(channel);

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

            _logger.Info("{ConnectionEndpoint}: Connected", channel.RemoteEndPoint);

            OnConnected(channel);
        }

        private void HandleDisconnect(NetPeerData peer, NetConnection connection, string reason)
        {
            var channel = _channels[connection];

            _logger.Info("{ConnectionEndpoint}: Disconnected ({DisconnectReason})", channel.RemoteEndPoint,
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
                _logger.Error("Caught exception in OnDisconnected handler:\n{0}", e);
            }
#endif
            _channels.Remove(connection);
            peer.RemoveChannel(channel);
            channel.EncryptionChannel?.Complete();

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
            DebugTools.Assert(msg.SenderConnection != null);

            var peer = msg.SenderConnection.Peer;
            if (peer.Status == NetPeerStatus.ShutdownRequested)
            {
                _logger.Warning($"{msg.SenderConnection.RemoteEndPoint}: Received data message, but shutdown is requested.");
                return true;
            }

            if (peer.Status == NetPeerStatus.NotRunning)
            {
                _logger.Warning($"{msg.SenderConnection.RemoteEndPoint}: Received data message, peer is not running.");
                return true;
            }

            if (!IsConnected)
            {
                _logger.Warning($"{msg.SenderConnection.RemoteEndPoint}: Received data message, but not connected.");
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
                _logger.Warning($"{msg.SenderConnection.RemoteEndPoint}: Received empty packet.");
                return true;
            }

            if (!_channels.TryGetValue(msg.SenderConnection, out var channel))
            {
                _logger.Warning($"{msg.SenderConnection.RemoteEndPoint}: Got unexpected data packet before handshake completion.");

                msg.SenderConnection.Disconnect("Unexpected packet before handshake completion");
                return true;
            }

            channel.Encryption?.Decrypt(msg);

            var id = msg.ReadByte();

            ref var entry = ref _netMsgIndices[id];

            if (entry == null)
            {
                _logger.Warning($"{msg.SenderConnection.RemoteEndPoint}: Got net message with invalid ID {id}.");

                channel.Disconnect("Got NetMessage with invalid ID");
                return true;
            }

            DebugTools.Assert(entry.Callback != null, $"Message is in {nameof(_netMsgIndices)} but doesn't have callback??");

            if (!channel.IsHandshakeComplete && !entry.IsHandshake)
            {
                _logger.Warning($"{msg.SenderConnection.RemoteEndPoint}: Got non-handshake message {entry.Type.Name} before handshake completion.");

                channel.Disconnect("Got unacceptable net message before handshake completion");
                return true;
            }

            var type = entry.Type;

            var instance = (NetMessage) Activator.CreateInstance(type)!;
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
                instance.ReadFromBuffer(msg, _serializer);
            }
            catch (InvalidCastException ice)
            {
                _logger.Error($"{msg.SenderConnection.RemoteEndPoint}: Wrong deserialization of {type.Name} packet:\n{ice}");
                return true;
            }
            catch (Exception e) // yes, we want to catch ALL exeptions for security
            {
                _logger.Error($"{msg.SenderConnection.RemoteEndPoint}: Failed to deserialize {type.Name} packet:\n{e}");
                return true;
            }

            if (_loggerPacket.IsLogLevelEnabled(LogLevel.Verbose))
                _loggerPacket.Verbose($"RECV: {instance.GetType().Name} {msg.LengthBytes}");

            try
            {
                entry.Callback!.Invoke(instance);
            }
            catch (Exception e)
            {
                _logger.Error($"{msg.SenderConnection.RemoteEndPoint}: exception in message handler for {type.Name}:\n{e}");
            }

            return true;
        }

        public void DispatchLocalNetMessage(NetMessage message)
        {
            if (!_messages.TryGetValue(message.MsgName, out var msgDat))
                return;

            msgDat.Callback!.Invoke(message);
        }

        private void CacheNetMsgIndex(int id, string name)
        {
            if (!_messages.TryGetValue(name, out var msgDat))
                return;

            if (msgDat.Callback == null)
                return;

            _netMsgIndices[id] = msgDat;
        }

        #region NetMessages

        /// <inheritdoc />
        public void RegisterNetMessage<T>(ProcessMessage<T>? rxCallback = null,
            NetMessageAccept accept = NetMessageAccept.Both)
            where T : NetMessage, new()
        {
            var name = new T().MsgName;
            var id = _strings.AddString(name);

            var data = new MessageData
            {
                Type = typeof(T),
                IsHandshake = (accept & NetMessageAccept.Handshake) != 0
            };

            _messages.Add(name, data);

            var thisSide = IsServer ? NetMessageAccept.Server : NetMessageAccept.Client;

            if (rxCallback != null && (accept & thisSide) != 0)
            {
                data.Callback = msg => rxCallback((T) msg);

                if (id != -1)
                    CacheNetMsgIndex(id, name);
            }
        }

        /// <inheritdoc />
        public T CreateNetMessage<T>()
            where T : NetMessage, new()
        {
            return new T();
        }

        private NetOutgoingMessage BuildMessage(NetMessage message, NetPeer peer)
        {
            var packet = peer.CreateMessage(4);

            if (!_strings.TryFindStringId(message.MsgName, out int msgId))
                throw new NetManagerException(
                    $"[NET] No string in table with name {message.MsgName}. Was it registered?");

            packet.Write((byte) msgId);
            message.WriteToBuffer(packet, _serializer);
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
            // TODO: Does the entity manager HAVE to shut down after network manager?
            // Though tbf theres no real point in sending messages anymore at that point.
            if (!_initialized)
                return;

            DebugTools.Assert(IsServer);
            if (!(recipient is NetChannel channel))
                throw new ArgumentException($"Not of type {typeof(NetChannel).FullName}", nameof(recipient));

            CoreSendMessage(channel, message);
        }

        private void LogSend(NetMessage message, NetDeliveryMethod method, NetOutgoingMessage packet)
        {
            if (_loggerPacket.IsLogLevelEnabled(LogLevel.Verbose))
                _loggerPacket.Verbose($"SEND: {message.GetType().Name} {method} {packet.LengthBytes}");
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

            var channel = peer.Channels[0];

            CoreSendMessage(channel, message);
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
        [Virtual]
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
        }

        private sealed class NetPeerData
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

        private sealed class MessageData
        {
            public bool IsHandshake;
            public Type Type = default!;
            public ProcessMessage? Callback;
        }

        void IPostInjectInit.PostInject()
        {
            _logger = _logMan.GetSawmill("net");
            _loggerPacket = _logMan.GetSawmill("net.packet");
            _authLogger = _logMan.GetSawmill("auth");
        }
    }

    /// <summary>
    ///     Generic exception thrown by the NetManager class.
    /// </summary>
    [Virtual]
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
