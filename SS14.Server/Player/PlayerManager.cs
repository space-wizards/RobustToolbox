using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameStates;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Players;
using SS14.Shared.Utility;

namespace SS14.Server.Player
{
    /// <summary>
    ///     This class will manage connected player sessions.
    /// </summary>
    internal class PlayerManager : IPlayerManager
    {
        [Dependency]
        private readonly IBaseServer _baseServer;

        [Dependency]
        private readonly IServerNetManager _network;
        [Dependency]
        private readonly IReflectionManager _reflectionManager;

        public BoundKeyMap KeyMap { get; private set; }

        private bool NeedsStateUpdate;

        /// <summary>
        ///     Active sessions of connected clients to the server.
        /// </summary>
        private Dictionary<NetSessionId, PlayerSession> _sessions;

        private Dictionary<NetSessionId, PlayerData> _playerData;

        /// <inheritdoc />
        public int PlayerCount => _sessions.Count;

        /// <inheritdoc />
        public int MaxPlayers { get; private set; } = 32;

        /// <inheritdoc />
        public event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        /// <inheritdoc />
        public void Initialize(int maxPlayers)
        {
            KeyMap = new BoundKeyMap(_reflectionManager);
            KeyMap.PopulateKeyFunctionsMap();

            MaxPlayers = maxPlayers;
            _sessions = new Dictionary<NetSessionId, PlayerSession>(maxPlayers);
            _playerData = new Dictionary<NetSessionId, PlayerData>(maxPlayers);

            _network.RegisterNetMessage<MsgSession>(MsgSession.NAME);
            _network.RegisterNetMessage<MsgServerInfoReq>(MsgServerInfoReq.NAME, HandleWelcomeMessageReq);
            _network.RegisterNetMessage<MsgServerInfo>(MsgServerInfo.NAME);
            _network.RegisterNetMessage<MsgPlayerListReq>(MsgPlayerListReq.NAME, HandlePlayerListReq);
            _network.RegisterNetMessage<MsgPlayerList>(MsgPlayerList.NAME);

            _network.Connecting += OnConnecting;
            _network.Connected += NewSession;
            _network.Disconnect += EndSession;
        }

        IPlayerSession IPlayerManager.GetSessionByChannel(INetChannel channel) => GetSessionByChannel(channel);
        private PlayerSession GetSessionByChannel(INetChannel channel)
        {
            // Should only be one session per client. Returns that session, in theory.
            return _sessions[channel.SessionId];
        }

        /// <inheritdoc />
        public IPlayerSession GetSessionById(NetSessionId index)
        {
            return _sessions[index];
        }

        public bool ValidSessionId(NetSessionId index)
        {
            return _sessions.ContainsKey(index);
        }

        public bool TryGetSessionById(NetSessionId sessionId, out IPlayerSession session)
        {
            if (_sessions.TryGetValue(sessionId, out var _session))
            {
                session = _session;
                return true;
            }
            session = default(IPlayerSession);
            return false;
        }

        /// <summary>
        ///     Causes all sessions to switch from the lobby to the the game.
        /// </summary>
        public void SendJoinGameToAll()
        {
            foreach (var s in _sessions.Values)
                s.JoinGame();
        }

        /// <summary>
        ///     Causes all sessions to switch from the game to the lobby.
        /// </summary>
        public void SendJoinLobbyToAll()
        {
            foreach (var s in _sessions.Values)
                s.JoinLobby();
        }

        /// <summary>
        ///     Causes all sessions to detach from their entity.
        /// </summary>
        public void DetachAll()
        {
            foreach (var s in _sessions.Values)
                s.DetachFromEntity();
        }

        /// <summary>
        ///     Gets all players inside of a circle.
        /// </summary>
        /// <param name="worldPos">Position of the circle in world-space.</param>
        /// <param name="range">Radius of the circle in world units.</param>
        /// <returns></returns>
        public List<IPlayerSession> GetPlayersInRange(GridLocalCoordinates worldPos, int range)
        {
            //TODO: This needs to be moved to the PVS system.
            return
                _sessions.Values.Where(x => x.AttachedEntity != null &&
                                     worldPos.InRange(x.AttachedEntity.GetComponent<ITransformComponent>().LocalPosition, range))
                    .Cast<IPlayerSession>()
                    .ToList();
        }

        /// <summary>
        ///     Gets all the players in the game lobby.
        /// </summary>
        /// <returns></returns>
        public List<IPlayerSession> GetPlayersInLobby()
        {
            //TODO: Lobby system needs to be moved to Content Assemblies.
            return
                _sessions.Values.Where(
                        x => x.Status == SessionStatus.InLobby)
                    .Cast<IPlayerSession>()
                    .ToList();
        }

        /// <summary>
        ///     Gets all players in the server.
        /// </summary>
        /// <returns></returns>
        public List<IPlayerSession> GetAllPlayers()
        {
            return _sessions.Values.Cast<IPlayerSession>().ToList();
        }

        /// <summary>
        ///     Gets all player states in the server.
        /// </summary>
        /// <returns></returns>
        public List<PlayerState> GetPlayerStates()
        {
            if (!NeedsStateUpdate)
            {
                return null;
            }
            NeedsStateUpdate = false;
            return _sessions.Values
                .Select(s => s.PlayerState)
                .ToList();
        }

        private void OnConnecting(object sender, NetConnectingArgs args)
        {
            if (PlayerCount >= _baseServer.MaxPlayers)
                args.Deny = true;
        }

        /// <summary>
        ///     Creates a new session for a client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void NewSession(object sender, NetChannelArgs args)
        {
            if (!_playerData.TryGetValue(args.Channel.SessionId, out var data))
            {
                data = new PlayerData(args.Channel.SessionId);
                _playerData.Add(args.Channel.SessionId, data);
            }
            var session = new PlayerSession(this, args.Channel, data);

            session.PlayerStatusChanged += (obj, sessionArgs) => OnPlayerStatusChanged(session, sessionArgs.OldStatus, sessionArgs.NewStatus);

            _sessions[args.Channel.SessionId] = session;
        }

        private void OnPlayerStatusChanged(IPlayerSession session, SessionStatus oldStatus, SessionStatus newStatus)
        {
            PlayerStatusChanged?.Invoke(this, new SessionStatusEventArgs(session, oldStatus, newStatus));
        }

        /// <summary>
        ///     Ends a clients session, and disconnects them.
        /// </summary>
        private void EndSession(object sender, NetChannelArgs args)
        {
            var session = GetSessionByChannel(args.Channel);

            // make sure nothing got messed up during the life of the session
            DebugTools.Assert(session.ConnectedClient == args.Channel);

            //Detach the entity and (don't)delete it.
            session.OnDisconnect();
            _sessions.Remove(session.SessionId);
        }

        private void HandleWelcomeMessageReq(MsgServerInfoReq message)
        {
            var session = GetSessionByChannel(message.MsgChannel);

            var netMsg = message.MsgChannel.CreateNetMessage<MsgServerInfo>();

            netMsg.ServerName = _baseServer.ServerName;
            netMsg.ServerPort = _network.Port;
            netMsg.ServerWelcomeMessage = _baseServer.Motd;
            netMsg.ServerMaxPlayers = _baseServer.MaxPlayers;
            netMsg.ServerMapName = _baseServer.MapName;
            netMsg.GameMode = _baseServer.GameModeName;
            netMsg.ServerPlayerCount = PlayerCount;
            netMsg.PlayerSessionId = session.SessionId;

            message.MsgChannel.SendMessage(netMsg);
        }

        private void HandlePlayerListReq(MsgPlayerListReq message)
        {
            var channel = message.MsgChannel;
            var players = GetAllPlayers().ToArray();
            var netMsg = channel.CreateNetMessage<MsgPlayerList>();

            var list = new List<PlayerState>();
            foreach (var client in players)
            {
                if (client == null)
                    continue;

                var info = new PlayerState
                {
                    SessionId = client.SessionId,
                    Name = client.Name,
                    Status = client.Status,
                    Ping = client.ConnectedClient.Ping
                };
                list.Add(info);
            }
            netMsg.Plyrs = list;
            netMsg.PlyCount = (byte)list.Count;

            channel.SendMessage(netMsg);

            // client session is complete
            var session = GetSessionByChannel(channel);
            session.Status = SessionStatus.Connected;
        }

        public void Dirty()
        {
            NeedsStateUpdate = true;
        }

        public IPlayerData GetPlayerData(NetSessionId sessionId)
        {
            return _playerData[sessionId];
        }

        public bool TryGetPlayerData(NetSessionId sessionId, out IPlayerData data)
        {
            if (_playerData.TryGetValue(sessionId, out var _data))
            {
                data = _data;
                return true;
            }
            data = default(IPlayerData);
            return false;
        }

        public bool HasPlayerData(NetSessionId sessionId)
        {
            return _playerData.ContainsKey(sessionId);
        }
    }

    public class SessionStatusEventArgs : EventArgs
    {
        public SessionStatusEventArgs(IPlayerSession session, SessionStatus oldStatus, SessionStatus newStatus)
        {
            Session = session;
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }

        public IPlayerSession Session { get; }
        public SessionStatus OldStatus { get; }
        public SessionStatus NewStatus { get; }
    }
}
