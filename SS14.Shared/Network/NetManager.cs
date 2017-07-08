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
    public class NetManager : INetClientManager, INetServerManager
    {
        private readonly Dictionary<Type, ProcessMessage> _callbacks = new Dictionary<Type, ProcessMessage>();

        /// <summary>
        ///     Holds the synced lookup table of NetConnection -> NetChannel
        /// </summary>
        private readonly Dictionary<NetConnection, NetChannel> _channels = new Dictionary<NetConnection, NetChannel>();

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

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public NetManager()
        {
            _config = IoCManager.Resolve<IConfigurationManager>();
            _config.RegisterCVar("net.port", 1212, CVarFlags.ARCHIVE);
            _config.RegisterCVar("net.allowdupeip", false, CVarFlags.ARCHIVE);
        }

        /// <inheritdoc />
        public int Port => _config.GetCVar<int>("net.port");

        /// <inheritdoc />
        public bool IsServer { get; private set; }

        /// <inheritdoc />
        public bool IsClient => !IsServer;

        /// <inheritdoc />
        public bool IsConnected => _netPeer.ConnectionsCount > 0;

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

            var config = IoCManager.Resolve<IConfigurationManager>();

            var netConfig = new NetPeerConfiguration("SS13_NetTag");

            if(isServer)
            {
                netConfig.Port = Port;
                netConfig.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            }

            if (!isServer)
            {
                config.RegisterCVar("net.server", "127.0.0.1", CVarFlags.ARCHIVE);
                config.RegisterCVar("net.updaterate", 20, CVarFlags.ARCHIVE);
                config.RegisterCVar("net.cmdrate", 30, CVarFlags.ARCHIVE);
                config.RegisterCVar("net.interpolation", 0.1f, CVarFlags.ARCHIVE);
                config.RegisterCVar("net.rate", 10240, CVarFlags.REPLICATED | CVarFlags.ARCHIVE);
            }

#if DEBUG
            config.RegisterCVar("net.fakelag", false, CVarFlags.CHEAT);
            config.RegisterCVar("net.fakeloss", 0.0f, CVarFlags.CHEAT);
            config.RegisterCVar("net.fakelagmin", 0.0f, CVarFlags.CHEAT);
            config.RegisterCVar("net.fakelagrand", 0.0f, CVarFlags.CHEAT);

            //Simulate Latency
            if (config.GetCVar<bool>("net.fakelag"))
            {
                netConfig.SimulatedLoss = config.GetCVar<float>("net.fakeloss");
                netConfig.SimulatedMinimumLatency = config.GetCVar<float>("net.fakelagmin");
                netConfig.SimulatedRandomLatency = config.GetCVar<float>("net.fakelagrand");
            }

            netConfig.ConnectionTimeout = 30000f;
#endif
            _netPeer = new NetPeer(netConfig);
            
            _netPeer.Start();
        }

        /// <inheritdoc />
        public void Shutdown(string reason)
        {
            foreach (var kvChannel in _channels)
                DisconnectChannel(kvChannel.Value, reason);

            _netPeer.Shutdown(reason);
        }

        /// <inheritdoc />
        public void Restart(string reason)
        {
            Shutdown(reason);
            Initialize(IsServer);
        }

        /// <summary>
        ///     Process incoming packets.
        /// </summary>
        public void ProcessPackets()
        {
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
        public void ClientConnect(string host)
        {
            Debug.Assert(_netPeer != null);
            Debug.Assert(!IsServer, "Should never be called on the server.");

            if (_netPeer.ConnectionsCount > 0)
                ClientDisconnect("Client left server.");

            _netPeer.Connect(host, 1212);
        }

        /// <inheritdoc />
        public void ClientDisconnect(string reason)
        {
            Debug.Assert(_netPeer != null);
            Debug.Assert(!IsServer, "Should never be called on the server.");

            // Client should never have more than one connection.
            Debug.Assert(_netPeer.ConnectionsCount <= 1);

            foreach (var connection in _netPeer.Connections)
            {
                connection.Disconnect(reason);
            }
        }

        /// <inheritdoc />
        public INetChannel ServerChannel
        {
            get
            {
                INetChannel retVal;

                if (_netPeer.ConnectionsCount <= 0)
                    return null;
                try
                {
                    retVal = GetChannel(_netPeer.Connections[0]);
                }
                catch
                {
                    // preempted!
                    return null;
                }
                return retVal;
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
                return null;

            if (_channels.TryGetValue(connection, out NetChannel channel))
                return channel;

            return null;
        }

        private void HandleStatusChanged(NetIncomingMessage msg)
        {
            var sender = msg.SenderConnection;
            var senderIp = sender.RemoteEndPoint.Address.ToString();
            Logger.Debug($"[NET] {senderIp}: Status changed to {sender.Status}");

            switch (sender.Status)
            {
                case NetConnectionStatus.Connected:
                    Logger.Info($"[NET] {senderIp}: Connected");
                    HandleConnected(sender);
                    break;

                case NetConnectionStatus.Disconnected:
                    Logger.Log("[NET]" + senderIp + ": Disconnected");

                    if (_channels.ContainsKey(sender))
                        HandleDisconnect(sender);
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

            OnConnected(channel);
        }

        private void HandleDisconnect(NetConnection sender)
        {
            var channel = _channels[sender];
            OnDisconnected(channel);
            _channels.Remove(sender);
        }

        private void DisconnectChannel(NetChannel channel, string reason)
        {
            OnDisconnected(channel);
            _channels.Remove(channel.Connection);
            channel.Connection.Disconnect(reason);
        }

        private void DispatchNetMessage(NetIncomingMessage msg)
        {
            //TODO: Convert client code to the new net message system, then remove this.
            if (!IsServer)
            {
                OnMessageArrived(msg);
                return;
            }

            var id = msg.ReadByte();

            if (!_strings.TryGetString(id, out string name))
                throw new Exception($"[NET] No string in table with ID {(NetMessages) id}. Did you register it?");

            if (!_messages.TryGetValue(name, out Type packetType))
                throw new Exception($"[NET] No message with Name {name}. Did you register it?");

            var channel = GetChannel(msg.SenderConnection);
            var instance = (NetMessage) Activator.CreateInstance(packetType, channel);
            instance.MsgChannel = channel;
            if (!_callbacks.TryGetValue(packetType, out ProcessMessage callback))
                return;

            instance.ReadFromBuffer(msg);
            callback?.Invoke(instance);
        }

        #region Packets

        public NetOutgoingMessage CreateMessage()
        {
            return _netPeer.CreateMessage();
        }

        public void ServerSendToAll(NetOutgoingMessage message, NetDeliveryMethod method)
        {
            foreach (var connection in _netPeer.Connections)
                ServerSendMessage(message, connection, method);
        }

        public void ServerSendMessage(NetOutgoingMessage message, NetConnection client, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            _netPeer.SendMessage(message, client, method);
        }

        public void SendToMany(NetOutgoingMessage message, List<NetConnection> recipients)
        {
            _netPeer.SendMessage(message, recipients, NetDeliveryMethod.ReliableOrdered, 0);
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
            return (T) Activator.CreateInstance(typeof(T), (INetChannel) null);
        }

        private NetOutgoingMessage BuildMessage(NetMessage message)
        {
            var packet = _netPeer.CreateMessage(4);

            if (! _strings.TryFindStringId(message.MsgName, out int msgId))
                throw new Exception($"[NET] No string in table with name {message.MsgName}. Was it registered?");

            packet.Write((byte) msgId);
            message.WriteToBuffer(packet);
            return packet;
        }

        /// <inheritdoc />
        public void ServerSendToAll(NetMessage message)
        {
            var packet = BuildMessage(message);
            ServerSendToAll(packet, NetDeliveryMethod.ReliableOrdered);
        }

        /// <inheritdoc />
        public void ServerSendMessage(NetMessage message, INetChannel recipient)
        {
            var packet = BuildMessage(message);
            ServerSendMessage(packet, recipient.Connection);
        }

        /// <inheritdoc />
        public void ServerSendToMany(NetMessage message, List<INetChannel> recipients)
        {
            foreach (var channel in recipients)
            {
                ServerSendMessage(message, channel);
            }
        }

        /// <inheritdoc />
        public void ClientSendMessage(NetOutgoingMessage message, NetDeliveryMethod deliveryMethod)
        {
            _netPeer.SendMessage(message, ServerChannel.Connection, deliveryMethod);
        }
        #endregion NetMessages

        #region Events

        protected virtual bool OnConnecting(string ip)
        {
            var args = new NetConnectingArgs(ip);
            Connecting?.Invoke(this, args);
            return !args.Deny;
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
        public event EventHandler<NetChannelArgs> Connected;

        /// <inheritdoc />
        public event EventHandler<NetChannelArgs> Disconnect;

        /// <inheritdoc />
        public event EventHandler<NetMessageArgs> MessageArrived;

        #endregion Events
    }
}
