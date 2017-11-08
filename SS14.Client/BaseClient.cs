using System;
using System.Diagnostics;
using Lidgren.Network;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.State;
using SS14.Client.State.States;
using SS14.Shared;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Client
{
    public class BaseClient : IBaseClient
    {
        [Dependency]
        private readonly IClientNetManager _net;

        [Dependency]
        private readonly IPlayerManager _playMan;

        /// <summary>
        ///     Default port that the client tries to connect to if no other port is specified.
        /// </summary>
        public ushort DefaultPort { get; } = 1212;

        public ClientRunLevel RunLevel { get; private set; }

        public ServerInfo GameInfo { get; private set; }

        public void Initialize()
        {
            _net.RegisterNetMessage<MsgServerInfo>(MsgServerInfo.NAME, (int) MsgServerInfo.ID, HandleServerInfo);

            _net.Connected += OnConnected;
            _net.ConnectFailed += OnConnectFailed;
            _net.Disconnect += OnNetDisconnect;

            _playMan.Initialize();

            Reset();
        }

        private void OnConnected(object sender, NetChannelArgs args)
        {
            // request base info about the server
            var msgInfo = _net.CreateNetMessage<MsgServerInfoReq>();
            // message is empty
            _net.ClientSendMessage(msgInfo, NetDeliveryMethod.ReliableOrdered);

            // start up player management
            _playMan.Startup(args.Channel);

            _playMan.LocalPlayer.StatusChanged += (obj, eventArgs) =>
            {
                if (eventArgs.NewStatus == SessionStatus.InLobby)
                {
                    var stateMan = IoCManager.Resolve<IStateManager>();
                    stateMan.RequestStateChange<Lobby>();
                }
            };
        }

        public void Update() { }

        public void Tick() { }

        public void Dispose() { }

        public void ConnectToServer(string ip, ushort port)
        {
            Debug.Assert(RunLevel < ClientRunLevel.Connect);
            Debug.Assert(!_net.IsConnected);

            OnRunLevelChanged(ClientRunLevel.Connect);
            _net.ClientConnect(ip, port);
        }

        public void DisconnectFromServer(string reason)
        {
            Debug.Assert(RunLevel > ClientRunLevel.Initialize);
            Debug.Assert(_net.IsConnected);

            // runlevel changed in OnNetDisconnect()
            _net.ClientDisconnect(reason);
        }

        public event EventHandler<RunLevelChangedEvent> RunLevelChanged;

        private void Reset()
        {
            OnRunLevelChanged(ClientRunLevel.Initialize);
        }
        
        private void OnConnectFailed(object sender, NetConnectFailArgs args)
        {
            Debug.Assert(RunLevel == ClientRunLevel.Connect);
            Reset();
        }

        private void OnNetDisconnect(object sender, NetChannelArgs args)
        {
            Debug.Assert(RunLevel > ClientRunLevel.Initialize);

            _playMan.Shutdown();

            Reset();
        }

        private void HandleServerInfo(NetMessage message)
        {
            if (GameInfo == null)
                GameInfo = new ServerInfo();

            var msg = (MsgServerInfo) message;
            var info = GameInfo;

            info.ServerName = msg.ServerName;
            info.ServerPort = msg.ServerPort;
            info.ServerWelcomeMessage = msg.ServerWelcomeMessage;
            info.ServerMaxPlayers = msg.ServerMaxPlayers;
            info.ServerMapName = msg.ServerMapName;
            info.GameMode = msg.GameMode;
            info.ServerPlayerCount = msg.ServerPlayerCount;

            // Server info is the first message to be sent by the server.
            // Receiving this message asserts that the connection was successful.
            Debug.Assert(RunLevel < ClientRunLevel.Lobby);
            OnRunLevelChanged(ClientRunLevel.Lobby);
        }

        private void OnRunLevelChanged(ClientRunLevel newRunLevel)
        {
            Logger.Debug($"[ENG] Runlevel changed to: {newRunLevel}");
            var evnt = new RunLevelChangedEvent(RunLevel, newRunLevel);
            RunLevel = newRunLevel;
            RunLevelChanged?.Invoke(this, evnt);
        }
    }

    public enum ClientRunLevel
    {
        Error = 0,
        Initialize,
        Connect,
        Lobby,
        Ingame
    }

    public class RunLevelChangedEvent : EventArgs
    {
        public ClientRunLevel OldLevel { get; }
        public ClientRunLevel NewLevel { get; }

        public RunLevelChangedEvent(ClientRunLevel oldLevel, ClientRunLevel newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }

    public class ServerInfo
    {
        public string ServerName { get; set; }
        public int ServerPort { get; set; }
        public string ServerWelcomeMessage { get; set; }
        public int ServerMaxPlayers { get; set; }
        public string ServerMapName { get; set; }
        public string GameMode { get; set; }
        public int ServerPlayerCount { get; set; }
    }
}
