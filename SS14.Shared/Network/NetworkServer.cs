using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lidgren.Network;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;

namespace SS14.Shared.Network
{
    public class NetworkServer : INetworkServer
    {
        /// <summary>
        ///     Holds the synced lookup table of Netchannel ID -> Netchannel
        /// </summary>
        private readonly Dictionary<NetConnection, NetChannel> _clients = new Dictionary<NetConnection, NetChannel>();

        private readonly Dictionary<string, Type> _messages = new Dictionary<string, Type>();

        /// <summary>
        ///     Holds a synced lookup table for NetMessages ID -> Name
        /// </summary>
        private readonly Dictionary<int, string> _messageStringTable = new Dictionary<int, string>();

        /// <summary>
        ///     The instance of the net server.
        /// </summary>
        private NetServer _netserver;

        private int _strTblIndex;

        public NetworkServer()
        {
            var cfgMgr = IoCManager.Resolve<IConfigurationManager>();
            cfgMgr.RegisterCVar("net.port", 1212);
        }

        public bool IsServer { get; private set; }

        [Obsolete]
        public NetServer Server { get; }

        [Obsolete]
        public NetPeerStatistics Statistics => _netserver.Statistics;

        public IEnumerable<NetChannel> Connections => _clients.Values.ToList();

        public NetChannel GetChannel(NetConnection connection)
        {
            if (_clients.TryGetValue(connection, out NetChannel channel))
                return channel;

            throw new Exception("");
        }

        public int ConnectionCount => _clients.Count;

        /// <summary>
        ///     Initializes the server.
        /// </summary>
        /// <param name="isServer"></param>
        public void Initialize(bool isServer)
        {
            IsServer = isServer;

            _netserver = new NetServer(LoadNetPeerConfig());
            _netserver.Start();
        }

        /// <summary>
        ///     Process incoming packets.
        /// </summary>
        public void ProcessPackets()
        {
            NetIncomingMessage msg;
            while ((msg = _netserver.ReadMessage()) != null)
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
                _netserver.Recycle(msg);
            }
        }

        /// <summary>
        ///     CLIENT
        ///     Gets the server NetChannel.
        /// </summary>
        /// <returns></returns>
        public NetChannel GetServerChannel()
        {
            throw new NotImplementedException();

            //TODO: This needs to be cleaned up
            if (IsServer)
                throw new Exception("[NET] Server should never be calling this!");

            //return _netserver.Connections[0];
        }

        /// <summary>
        ///     Handle a NetChannel status changing.
        /// </summary>
        /// <param name="msg">Status message.</param>
        private void HandleStatusChanged(NetIncomingMessage msg)
        {
            var sender = msg.SenderConnection;
            var senderIp = sender.RemoteEndPoint.Address.ToString();
            Logger.Log($"[NET] {senderIp}: Status changed to {sender.Status}", LogLevel.Debug);

            switch (sender.Status)
            {
                case NetConnectionStatus.Connected:
                    Logger.Info($"[NET] {senderIp}: Connected");

                    if (_clients.ContainsKey(sender)) // TODO Move this to a config to allow or disallowed shared IPAddress
                    {
                        Logger.Error("[NET] " + senderIp + ": Already connected");
                        sender.Disconnect("Duplicate connection.");
                        return;
                    }

                    HandleConnectionApproval(sender);
                    //IoCManager.Resolve<IPlayerManager>().NewSession(sender);
                    // TODO move this to somewhere that makes more sense.

                    break;

                case NetConnectionStatus.Disconnected:
                    Logger.Log("[NET]" + senderIp + ": Disconnected");

                    //IoCManager.Resolve<IPlayerManager>().EndSession(sender);
                    if (_clients.ContainsKey(sender))
                        CleanupClientConnection(sender);
                    break;
            }
        }

        private void HandleConnectionApproval(NetConnection sender)
        {
            _clients.Add(sender, new NetChannel(sender));
        }

        private void CleanupClientConnection(NetConnection sender)
        {
            _clients.Remove(sender);
        }

        private void DispatchNetMessage(NetIncomingMessage msg)
        {
            var id = msg.ReadByte();
            if (TryGetStringName(id, out string name))
            {
                if (_messages.TryGetValue(name, out Type packetType))
                {
                    var channel = GetChannel(msg.SenderConnection);
                    var instance = (NetMessage) Activator.CreateInstance(packetType, channel);
                    if (instance.Callback != null)
                    {
                        instance.ReadFromBuffer(msg);
                        instance.Callback?.Invoke(instance);
                    }
                    return;
                }
                throw new Exception($"[NET] No message with Name {name}. Did you register it?");
            }
            throw new Exception($"[NET] No string in table with ID {id}. Did you register it?");
        }

        private static NetPeerConfiguration LoadNetPeerConfig()
        {
            var cfgMgr = IoCManager.Resolve<IConfigurationManager>();
            var _config = new NetPeerConfiguration("SS13_NetTag");
            _config.Port = cfgMgr.GetCVar<int>("net.port");
#if DEBUG
            _config.ConnectionTimeout = 30000f;
#endif

            return _config;
        }

        #region Packets

        [Obsolete]
        public void SendToAll(NetOutgoingMessage message)
        {
            _netserver.SendToAll(message, NetDeliveryMethod.ReliableOrdered);
        }

        [Obsolete]
        public void SendMessage(NetOutgoingMessage message, NetConnection client)
        {
            _netserver.SendMessage(message, client, NetDeliveryMethod.ReliableOrdered);
        }

        [Obsolete]
        public void SendToMany(NetOutgoingMessage message, List<NetConnection> recipients)
        {
            _netserver.SendMessage(message, recipients, NetDeliveryMethod.ReliableOrdered, 0);
        }


        public void SendToAll(NetMessage message)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(NetMessage message, NetChannel client)
        {
            throw new NotImplementedException();
        }

        public void SendToMany(NetMessage message, List<NetChannel> recipients)
        {
            throw new NotImplementedException();
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

        public void RegisterNetMessage<T>(string name, int id, NetMessage.ProcessMessage func = null)
            where T : NetMessage
        {
            _messageStringTable.Add(id, name);

            _messages.Add(name, typeof(T));

            if (func == null)
                return;

            //cringe...
            var field = typeof(T).GetField("_callback", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            field.SetValue(null, func);
        }

        public NetOutgoingMessage CreateMessage(int capacity = 4)
        {
            return _netserver.CreateMessage(capacity);
        }

        #endregion NetMessages

        #region Events

        public delegate void OnConnectingEvent();

        public event OnConnectingEvent OnConnecting;

        public delegate void OnConnectedEvent(NetChannel channel);
        public event OnConnectedEvent OnConnected;

        public delegate void OnDisconnectEvent();

        public event OnDisconnectEvent OnDisconnect;

        #endregion Events
    }
}
