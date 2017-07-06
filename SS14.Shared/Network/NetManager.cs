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
        ///     Holds a synced lookup table for NetMessage.Id -> NetMessage.Name
        /// </summary>
        private readonly Dictionary<int, string> _messageStringTable = new Dictionary<int, string>();

        /// <summary>
        ///     The instance of the net server.
        /// </summary>
        protected NetPeer NetPeer;

        private int _strTblIndex;

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
        public bool IsServer { get; private set; }

        /// <inheritdoc />
        public bool IsConnected => NetPeer.ConnectionsCount > 0;

        /// <inheritdoc />
        [Obsolete("You should be using NetPeer.")]
        public NetPeer Peer => NetPeer;

        /// <inheritdoc />
        [Obsolete]
        public NetPeerStatistics Statistics => NetPeer.Statistics;

        /// <inheritdoc />
        public List<INetChannel> Channels => _channels.Values.Cast<INetChannel>().ToList();

        /// <inheritdoc />
        public int ChannelCount => _channels.Count;

        /// <inheritdoc />
        public void Initialize(bool isServer)
        {
            if (NetPeer != null)
                throw new InvalidOperationException("[NET] NetManager has already been initialized.");

            IsServer = isServer;

            var config = IoCManager.Resolve<IConfigurationManager>();

            var netConfig = new NetPeerConfiguration("SS13_NetTag");

            if(isServer)
                netConfig.Port = config.GetCVar<int>("net.port");

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
            NetPeer = new NetPeer(netConfig);
            
            NetPeer.Start();
        }

        /// <inheritdoc />
        public void Shutdown(string reason)
        {
            //TODO: Call Disconnect hook for each channel.

            NetPeer.Shutdown(reason);
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
            Debug.Assert(NetPeer != null);

            NetIncomingMessage msg;
            while ((msg = NetPeer.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.VerboseDebugMessage:
                        Logger.Log(msg.ReadString(), LogLevel.Debug);
                        break;

                    case NetIncomingMessageType.DebugMessage:
                        Logger.Log(msg.ReadString(), LogLevel.Debug);
                        break;

                    case NetIncomingMessageType.WarningMessage:
                        Logger.Log(msg.ReadString(), LogLevel.Warning);
                        break;

                    case NetIncomingMessageType.ErrorMessage:
                        Logger.Log(msg.ReadString(), LogLevel.Error);
                        break;

                    case NetIncomingMessageType.Data:
                        DispatchNetMessage(msg);
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        HandleStatusChanged(msg);
                        break;

                    default:
                        Logger.Log("[NET] Unhandled incoming packet type: " + msg.MessageType, LogLevel.Warning);
                        break;
                }
                NetPeer.Recycle(msg);
            }
        }

        /// <inheritdoc />
        public void ClientConnect(string host)
        {
            Debug.Assert(NetPeer != null);
            Debug.Assert(!IsServer, "Should never be called on the server.");

            if (NetPeer.ConnectionsCount > 0)
                ClientDisconnect("Client left server.");

            NetPeer.Connect(host, 1212);
        }

        /// <inheritdoc />
        public void ClientDisconnect(string reason)
        {
            Debug.Assert(NetPeer != null);
            Debug.Assert(!IsServer, "Should never be called on the server.");

            // Client should never have more than one connection.
            Debug.Assert(NetPeer.ConnectionsCount <= 1);

            foreach (var connection in NetPeer.Connections)
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

                if (NetPeer.ConnectionsCount <= 0)
                    return null;
                try
                {
                    retVal = GetChannel(NetPeer.Connections[0]);
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
            Logger.Log($"[NET] {senderIp}: Status changed to {sender.Status}", LogLevel.Debug);

            switch (sender.Status)
            {
                case NetConnectionStatus.Connected:
                    Logger.Info($"[NET] {senderIp}: Connected");

                    // TODO: Move this to Connecting status
                    if (_config.GetCVar<bool>("net.allowdupeip") && _channels.ContainsKey(sender))
                    {
                        Logger.Error("[NET] " + senderIp + ": Already connected");
                        sender.Disconnect("Duplicate connection.");
                        return;
                    }
                    HandleConnectionApproval(sender);
                    break;

                case NetConnectionStatus.Disconnected:
                    Logger.Log("[NET]" + senderIp + ": Disconnected");

                    if (_channels.ContainsKey(sender))
                        CleanupClientConnection(sender);
                    break;
            }
        }

        private void HandleConnectionApproval(NetConnection sender)
        {
            var channel = new NetChannel(this, sender);
            _channels.Add(sender, channel);

            OnConnected(channel);
        }

        private void CleanupClientConnection(NetConnection sender)
        {
            var channel = _channels[sender];
            OnDisconnected(channel);
            _channels.Remove(sender);
        }

        private void DispatchNetMessage(NetIncomingMessage msg)
        {
            //TODO: Literally kill this with fire
            if (!IsServer)
            {
                OnMessageArrived(msg);
                return;
            }

            var id = msg.ReadByte();

            if (!_messageStringTable.TryGetValue(id, out string name))
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
            return NetPeer.CreateMessage();
        }

        public void ServerSendToAll(NetOutgoingMessage message, NetDeliveryMethod method)
        {
            foreach (var connection in NetPeer.Connections)
                ServerSendMessage(message, connection, method);
        }

        public void ServerSendMessage(NetOutgoingMessage message, NetConnection client, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            NetPeer.SendMessage(message, client, method);
        }

        public void SendToMany(NetOutgoingMessage message, List<NetConnection> recipients)
        {
            NetPeer.SendMessage(message, recipients, NetDeliveryMethod.ReliableOrdered, 0);
        }

        #endregion Packets

        #region StringTable

        public void AddString(string name, bool overwrite = false)
        {
            if (overwrite && TryFindStringId(name, out int id))
                throw new Exception($"[NET] StringTable already contains the string '{name}'.");
            var newID = IsServer ? ++_strTblIndex : --_strTblIndex; // Clients string table will be overwritten
            _messageStringTable.Add(newID, name);
        }

        public bool TryFindStringId(string str, out int id)
        {
            // Does this need a better ADT?
            var keys = _messageStringTable.Where(kvp => kvp.Value == str).Select(kvp => kvp.Key).ToList();
            if (keys.Any())
            {
                id = keys.First();
                return true;
            }
            id = 0;
            return false;
        }

        public bool TryGetStringName(int id, out string name)
        {
            return _messageStringTable.TryGetValue(id, out name);
        }

        #endregion StringTable

        #region NetMessages

        /// <inheritdoc />
        public void RegisterNetMessage<T>(string name, int id, ProcessMessage rxCallback = null)
            where T : NetMessage
        {
            _messageStringTable.Add(id, name);

            _messages.Add(name, typeof(T));

            if (rxCallback == null)
                return;

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
            var packet = NetPeer.CreateMessage(4);

            if (!TryFindStringId(message.MsgName, out int msgId))
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
            NetPeer.SendMessage(message, ServerChannel.Connection, deliveryMethod);
        }
        #endregion NetMessages

        #region Events

        protected virtual void OnConnecting(INetChannel channel)
        {
            Connecting?.Invoke(this, new NetChannelArgs(channel));
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
        public event EventHandler<NetChannelArgs> Connecting;

        /// <inheritdoc />
        public event EventHandler<NetChannelArgs> Connected;

        /// <inheritdoc />
        public event EventHandler<NetChannelArgs> Disconnect;

        /// <inheritdoc />
        public event EventHandler<NetMessageArgs> MessageArrived;

        #endregion Events
    }
}
