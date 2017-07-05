using System;
using System.Collections.Generic;
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
    ///     A peer is in the process of connecting.
    /// </summary>
    /// <param name="channel"></param>
    public delegate void OnConnectingEvent(INetChannel channel);

    /// <summary>
    ///     Global event for when a peer connects. Use this to set up per-peer info.
    /// </summary>
    /// <param name="channel"></param>
    public delegate void OnConnectedEvent(INetChannel channel);

    /// <summary>
    ///     Global event for when a peer disconnects.
    /// </summary>
    /// <param name="channel">The NetChannel of the peer that disconnected.</param>
    public delegate void OnDisconnectEvent(INetChannel channel);

    /// <summary>
    ///     Callback for registered NetMessages.
    /// </summary>
    /// <param name="message">The message received.</param>
    public delegate void ProcessMessage(NetMessage message);

    /// <summary>
    ///     Manages all network connections and packet IO.
    /// </summary>
    public class NetManager : INetManager
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
        private NetServer _netServer;

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
        [Obsolete("You should be using _server.")]
        public NetServer Server => _netServer;

        /// <inheritdoc />
        [Obsolete]
        public NetPeerStatistics Statistics => _netServer.Statistics;

        /// <inheritdoc />
        public List<INetChannel> Channels => _channels.Values.Cast<INetChannel>().ToList();

        /// <inheritdoc />
        public int ChannelCount => _channels.Count;

        /// <inheritdoc />
        public void Initialize(bool isServer)
        {
            IsServer = isServer;

            var cfgMgr = IoCManager.Resolve<IConfigurationManager>();
            var config = new NetPeerConfiguration("SS13_NetTag");
            config.Port = cfgMgr.GetCVar<int>("net.port");
#if DEBUG
            config.ConnectionTimeout = 30000f;
#endif

            _netServer = new NetServer(config);
            _netServer.Start();
        }

        /// <summary>
        ///     Process incoming packets.
        /// </summary>
        public void ProcessPackets()
        {
            NetIncomingMessage msg;
            while ((msg = _netServer.ReadMessage()) != null)
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
                _netServer.Recycle(msg);
            }
        }

        /// <inheritdoc />
        public INetChannel GetServerChannel()
        {
            //TODO: This is a client feature, which is not supported yet.
            throw new NotImplementedException();

            if (IsServer)
                throw new Exception("[NET] Server should never be calling this!");
        }

        /// <summary>
        ///     Gets the NetChannel of a peer NetConnection.
        /// </summary>
        /// <param name="connection">The raw connection of the peer.</param>
        /// <returns>The NetChannel of the peer.</returns>
        private INetChannel GetChannel(NetConnection connection)
        {
            if (_channels.TryGetValue(connection, out NetChannel channel))
                return channel;

            throw new Exception("");
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

                    // TODO: Move this to OnConnecting status
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

            OnConnected?.Invoke(channel);
        }

        private void CleanupClientConnection(NetConnection sender)
        {
            var channel = _channels[sender];
            OnDisconnect?.Invoke(channel);
            _channels.Remove(sender);
        }

        private void DispatchNetMessage(NetIncomingMessage msg)
        {
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

        private void SendToAll(NetOutgoingMessage message)
        {
            _netServer.SendToAll(message, NetDeliveryMethod.ReliableOrdered);
        }

        private void SendMessage(NetOutgoingMessage message, NetConnection client)
        {
            _netServer.SendMessage(message, client, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendToMany(NetOutgoingMessage message, List<NetConnection> recipients)
        {
            _netServer.SendMessage(message, recipients, NetDeliveryMethod.ReliableOrdered, 0);
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
            var packet = _netServer.CreateMessage(4);

            if (!TryFindStringId(message.MsgName, out int msgId))
                throw new Exception($"[NET] No string in table with name {message.MsgName}. Was it registered?");

            packet.Write((byte) msgId);
            message.WriteToBuffer(packet);
            return packet;
        }

        /// <inheritdoc />
        public void SendToAll(NetMessage message)
        {
            var packet = BuildMessage(message);
            SendToAll(packet);
        }

        /// <inheritdoc />
        public void SendMessage(NetMessage message, INetChannel recipient)
        {
            var packet = BuildMessage(message);
            SendMessage(packet, recipient.Connection);
        }

        /// <inheritdoc />
        public void SendToMany(NetMessage message, List<INetChannel> recipients)
        {
            throw new NotImplementedException();
        }

        #endregion NetMessages

        #region Events

        /// <inheritdoc />
        public event OnConnectingEvent OnConnecting;

        /// <inheritdoc />
        public event OnConnectedEvent OnConnected;

        /// <inheritdoc />
        public event OnDisconnectEvent OnDisconnect;

        #endregion Events
    }
}
