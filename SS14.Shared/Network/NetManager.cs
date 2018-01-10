using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lidgren.Network;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;

namespace SS14.Shared.Network
{
    /// <summary>
    ///     Callback for registered NetMessages.
    /// </summary>
    /// <param name="message">The message received.</param>
    public delegate void ProcessMessage(NetMessage message);

    /// <summary>
    ///     Manages all network connections and packet IO.
    /// </summary>
    public class NetManager : IClientNetManager, IServerNetManager, IDisposable
    {
        private readonly Dictionary<Type, ProcessMessage> _callbacks = new Dictionary<Type, ProcessMessage>();

        /// <summary>
        ///     Holds the synced lookup table of NetConnection -> NetChannel
        /// </summary>
        private readonly Dictionary<NetConnection, NetChannel> _channels = new Dictionary<NetConnection, NetChannel>();

        [Dependency]
        private readonly IConfigurationManager _config;

        /// <summary>
        ///     Holds lookup table for NetMessage.Id -> NetMessage.Type
        /// </summary>
        private readonly Dictionary<string, Type> _messages = new Dictionary<string, Type>();

        /// <summary>
        /// The StringTable for transforming packet Ids to Packet name.
        /// </summary>
        private readonly StringTable _strings = new StringTable();

        /// <summary>
        ///     The instance of the net server.
        /// </summary>
        private NetPeer _netPeer;

        /// <inheritdoc />
        public int Port => _config.GetCVar<int>("net.port");

        /// <inheritdoc />
        public bool IsServer { get; private set; }

        /// <inheritdoc />
        public bool IsClient => !IsServer;

        /// <inheritdoc />
        public bool IsConnected => _netPeer != null && _netPeer.ConnectionsCount > 0;

        public bool IsRunning => _netPeer != null;

        /// <inheritdoc />
        [Obsolete("You should be using NetPeer.")]
        public NetPeer Peer => _netPeer;

        /// <inheritdoc />
        [Obsolete]
        public NetPeerStatistics Statistics => _netPeer.Statistics;

        /// <inheritdoc />
        public List<INetChannel> Channels => _channels.Values.Cast<INetChannel>().ToList();

        /// <inheritdoc />
        public int ChannelCount => _channels.Count;

        /// <inheritdoc />
        public void Initialize(bool isServer)
        {
            if (_netPeer != null)
                throw new InvalidOperationException("[NET] NetManager has already been initialized.");

            IsServer = isServer;

            _config.RegisterCVar("net.port", 1212, CVar.ARCHIVE);
            _config.RegisterCVar("net.allowdupeip", false, CVar.ARCHIVE);

            if (!isServer)
            {
                _config.RegisterCVar("net.server", "127.0.0.1", CVar.ARCHIVE);
                _config.RegisterCVar("net.updaterate", 20, CVar.ARCHIVE);
                _config.RegisterCVar("net.cmdrate", 30, CVar.ARCHIVE);
                _config.RegisterCVar("net.interpolation", 0.1f, CVar.ARCHIVE);
                _config.RegisterCVar("net.rate", 10240, CVar.REPLICATED | CVar.ARCHIVE);
            }

#if DEBUG
            _config.RegisterCVar("net.fakelag", false, CVar.CHEAT);
            _config.RegisterCVar("net.fakeloss", 0.0f, CVar.CHEAT);
            _config.RegisterCVar("net.fakelagmin", 0.0f, CVar.CHEAT);
            _config.RegisterCVar("net.fakelagrand", 0.0f, CVar.CHEAT);
#endif

            _strings.Initialize(this, () =>
            {
                OnConnected(ServerChannel);
            });
        }

        public void Startup()
        {
            var netConfig = new NetPeerConfiguration("SS13_NetTag");

            if (IsServer)
            {
                netConfig.Port = Port;
                netConfig.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            }

#if DEBUG
            //Simulate Latency
            if (_config.GetCVar<bool>("net.fakelag"))
            {
                netConfig.SimulatedLoss = _config.GetCVar<float>("net.fakeloss");
                netConfig.SimulatedMinimumLatency = _config.GetCVar<float>("net.fakelagmin");
                netConfig.SimulatedRandomLatency = _config.GetCVar<float>("net.fakelagrand");
            }

            netConfig.ConnectionTimeout = 30000f;
#endif
            _netPeer = new NetPeer(netConfig);

            _netPeer.Start();
        }

        public void Dispose()
        {
            if (_netPeer != null)
            {
                Shutdown("Network manager getting disposed.");
            }
        }

        /// <inheritdoc />
        public void Shutdown(string reason)
        {
            foreach (var kvChannel in _channels)
                DisconnectChannel(kvChannel.Value, reason);

            _netPeer.Shutdown(reason);
            _strings.Reset();
        }

        /// <inheritdoc />
        public void Restart(string reason)
        {
            Shutdown(reason);
            Startup();
        }

        /// <summary>
        ///     Process incoming packets.
        /// </summary>
        public void ProcessPackets()
        {
            // client does not always have its networking running, for example on main menu
            if (IsClient && _netPeer == null)
                return;

            // server on the other hand needs it to be running
            Debug.Assert(_netPeer != null);

            NetIncomingMessage msg;
            while ((msg = _netPeer.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.VerboseDebugMessage:
                        Logger.Debug($"[NET] {msg.ReadString()}");
                        break;

                    case NetIncomingMessageType.DebugMessage:
                        Logger.Info("[NET] " + msg.ReadString());
                        break;

                    case NetIncomingMessageType.WarningMessage:
                        Logger.Warning("[NET] " + msg.ReadString());
                        break;

                    case NetIncomingMessageType.ErrorMessage:
                        Logger.Error("[NET] " + msg.ReadString());
                        break;

                    case NetIncomingMessageType.ConnectionApproval:
                        HandleApproval(msg);
                        break;

                    case NetIncomingMessageType.Data:
                        DispatchNetMessage(msg);
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        HandleStatusChanged(msg);
                        break;

                    default:
                        Logger.Warning($"[NET] {msg.SenderConnection.RemoteEndPoint.Address}: Unhandled incoming packet type: {msg.MessageType}");
                        break;
                }
                _netPeer.Recycle(msg);
            }
        }

        /// <inheritdoc />
        public void ClientConnect(string host, int port)
        {
            Debug.Assert(_netPeer != null);
            Debug.Assert(!IsServer, "Should never be called on the server.");
            Debug.Assert(!IsConnected);

            if (_netPeer.ConnectionsCount > 0)
                ClientDisconnect("Client left server.");

            Logger.Info($"[NET] Connecting to {host}:{port}...");
            _netPeer.Connect(host, port);
        }

        /// <inheritdoc />
        public void ClientDisconnect(string reason)
        {
            Debug.Assert(IsClient, "Should never be called on the server.");

            if (_netPeer == null)
                return;

            // Client should never have more than one connection.
            Debug.Assert(_netPeer.ConnectionsCount <= 1);

            foreach (var connection in _netPeer.Connections)
            {
                connection.Disconnect(reason);
            }

            Shutdown(reason);
        }

        /// <inheritdoc />
        public INetChannel ServerChannel => _netPeer.ConnectionsCount > 0 ? GetChannel(_netPeer.Connections[0]) : null;

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

        private void HandleStatusChanged(NetIncomingMessage msg)
        {
            var sender = msg.SenderConnection;
            var senderIp = sender.RemoteEndPoint.Address.ToString();
            Logger.Debug($"[NET] {senderIp}: Status changed to {sender.Status}");

            switch (sender.Status)
            {
                case NetConnectionStatus.Connected:
                    HandleConnected(sender);
                    break;

                case NetConnectionStatus.Disconnected:
                    if (_channels.ContainsKey(sender))
                        HandleDisconnect(msg);
                    else if (sender.RemoteUniqueIdentifier == 0) // is this the best way to detect an unsuccessful connect?
                    {
                        Logger.Info($"[NET] {sender.RemoteEndPoint}: Failed to connect");
                        OnConnectFailed();
                    }
                    break;
            }
        }

        private void HandleApproval(NetIncomingMessage message)
        {
            var sender = message.SenderConnection;
            var ip = sender.RemoteEndPoint.Address;
            if (!_config.GetCVar<bool>("net.allowdupeip") && _channels.Any(item => Equals(item.Key.RemoteEndPoint.Address, ip)))
            {
                Logger.Info($"[NET] {sender.RemoteEndPoint.Address}: Already connected.");
                sender.Deny("Duplicate connection.");
            }
            else
            {
                if (OnConnecting(ip.ToString()))
                    message.SenderConnection.Approve();
                else
                    sender.Deny("Server is full.");
            }
        }

        private void HandleConnected(NetConnection sender)
        {
            var channel = new NetChannel(this, sender);
            _channels.Add(sender, channel);

            _strings.SendFullTable(channel);

            Logger.Info($"[NET] {channel.RemoteAddress}: Connected");

            // client is connected after string packet get received
            if (IsServer)
                OnConnected(channel);
        }

        private void HandleDisconnect(NetIncomingMessage message)
        {
            string reason;
            try
            {
                message.ReadByte(); // status
                reason = message.ReadString();
            }
            catch (NetException)
            {
                reason = String.Empty;
            }

            var conn = message.SenderConnection;
            var channel = _channels[conn];

            Logger.Info($"[NET] {channel.RemoteAddress}:{channel.Connection.RemoteEndPoint.Port}: Disconnected ({reason})");

            OnDisconnected(channel);
            _channels.Remove(conn);

            if (IsClient)
                _strings.Reset();
        }

        // server-side disconnect
        private void DisconnectChannel(NetChannel channel, string reason)
        {
            channel.Connection.Disconnect(reason);
        }

        private void DispatchNetMessage(NetIncomingMessage msg)
        {
            string address = msg.SenderConnection.RemoteEndPoint.Address.ToString();

            if (msg.LengthBytes < 1)
            {
                Logger.Warning($"[NET] {address}: Received empty packet.");
                return;
            }

            var id = msg.ReadByte();

            if (!_strings.TryGetString(id, out string name))
            {
                //If the message was not registered, fallback to the old broadcast event on the client.
                //TODO: Convert client code to the new net message system, then remove this.
                if (IsClient)
                {
                    msg.Position = 0;
                    OnMessageArrived(msg);
                    return;
                }

                Logger.Warning($"[NET] {address}:  No string in table with ID {(NetMessages)id}.");
                return;
            }

            if (!_messages.TryGetValue(name, out Type packetType))
            {
                //If the message was not registered, fallback to the old broadcast event on the client.
                //TODO: Convert client code to the new net message system, then remove this.
                if (IsClient)
                {
                    msg.Position = 0;
                    OnMessageArrived(msg);
                    return;
                }

                Logger.Warning($"[NET] {address}: No message with Name {name}.");
                return;
            }

            var channel = GetChannel(msg.SenderConnection);
            var instance = (NetMessage)Activator.CreateInstance(packetType, channel);
            instance.MsgChannel = channel;

            try
            {
                instance.ReadFromBuffer(msg);
            }
            catch (Exception e) // yes, we want to catch ALL exeptions for security
            {
                Logger.Warning($"[NET] {address}: Failed to deserialize {packetType.Name} packet:\n{e}");
            }

            DebugIn(instance);

            if (!_callbacks.TryGetValue(packetType, out ProcessMessage callback))
                return;

            callback?.Invoke(instance);
        }

        #region Packets

        public NetOutgoingMessage CreateMessage()
        {
            return _netPeer.CreateMessage();
        }

        public void ServerSendToAll(NetOutgoingMessage message, NetDeliveryMethod method)
        {
            if (_netPeer.Connections.Count > 0)
                _netPeer.SendMessage(message, _netPeer.Connections, method, 0);
        }

        public void ServerSendMessage(NetOutgoingMessage message, NetConnection client, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            _netPeer.SendMessage(message, client, method);
        }

        #endregion Packets

        #region NetMessages

        /// <inheritdoc />
        public void RegisterNetMessage<T>(string name, int id, ProcessMessage rxCallback = null)
            where T : NetMessage
        {
            _strings.AddStringFixed(id, name);

            _messages.Add(name, typeof(T));

            if (rxCallback != null)
                _callbacks.Add(typeof(T), rxCallback);
        }

        /// <inheritdoc />
        public T CreateNetMessage<T>()
            where T : NetMessage
        {
            return (T)Activator.CreateInstance(typeof(T), (INetChannel)null);
        }

        private NetOutgoingMessage BuildMessage(NetMessage message)
        {
            var packet = _netPeer.CreateMessage(4);

            if (!_strings.TryFindStringId(message.MsgName, out int msgId))
                throw new NetManagerException($"[NET] No string in table with name {message.MsgName}. Was it registered?");

            packet.Write((byte)msgId);
            message.WriteToBuffer(packet);
            return packet;
        }

        /// <inheritdoc />
        public void ServerSendToAll(NetMessage message)
        {
            if (_netPeer == null)
                return;

            var packet = BuildMessage(message);
            ServerSendToAll(packet, NetDeliveryMethod.ReliableOrdered);
        }

        /// <inheritdoc />
        public void ServerSendMessage(NetMessage message, INetChannel recipient)
        {
            if (_netPeer == null)
                return;

            DebugOut(message);
            var packet = BuildMessage(message);
            ServerSendMessage(packet, recipient.Connection);
        }

        /// <inheritdoc />
        public void ServerSendToMany(NetMessage message, List<INetChannel> recipients)
        {
            if (_netPeer == null)
                return;

            foreach (var channel in recipients)
            {
                ServerSendMessage(message, channel);
            }
        }

        /// <inheritdoc />
        public void ClientSendMessage(NetMessage message, NetDeliveryMethod deliveryMethod)
        {
            if (_netPeer == null)
                return;

            var packet = BuildMessage(message);
            ClientSendMessage(packet, deliveryMethod);
        }

        /// <inheritdoc />
        public void ClientSendMessage(NetOutgoingMessage message, NetDeliveryMethod deliveryMethod)
        {
            if (_netPeer == null)
                return;

            // not connected to a server, so a message cannot be sent to it.
            if (ServerChannel == null)
                return;

            _netPeer.SendMessage(message, ServerChannel.Connection, deliveryMethod);
        }
        #endregion NetMessages

        #region NetDebug

        [Conditional("DEBUG")]
        private void DebugOut(NetMessage msg)
        {
            //Logger.Debug($"[NET] OUT: {msg.MsgName}");
        }

        [Conditional("DEBUG")]
        private void DebugIn(NetMessage msg)
        {
            //Logger.Debug($"[NET]  IN: {msg.MsgName}");
        }

        #endregion NetDebug

        #region Events

        protected virtual bool OnConnecting(string ip)
        {
            var args = new NetConnectingArgs(ip);
            Connecting?.Invoke(this, args);
            return !args.Deny;
        }

        protected virtual void OnConnectFailed()
        {
            var args = new NetConnectFailArgs();
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

        protected virtual void OnMessageArrived(NetIncomingMessage message)
        {
            MessageArrived?.Invoke(this, new NetMessageArgs(null, message));
        }

        /// <inheritdoc />
        public event EventHandler<NetConnectingArgs> Connecting;

        /// <inheritdoc />
        public event EventHandler<NetConnectFailArgs> ConnectFailed;

        /// <inheritdoc />
        public event EventHandler<NetChannelArgs> Connected;

        /// <inheritdoc />
        public event EventHandler<NetChannelArgs> Disconnect;

        /// <inheritdoc />
        public event EventHandler<NetMessageArgs> MessageArrived;

        #endregion Events
    }

    /// <summary>
    ///     Generic exception thrown by the NetManager class.
    /// </summary>
    public class NetManagerException : Exception
    {
        public NetManagerException()
        {
        }

        public NetManagerException(string message)
            : base(message)
        {
        }

        public NetManagerException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
