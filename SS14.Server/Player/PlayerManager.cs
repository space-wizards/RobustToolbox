using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Players;

namespace SS14.Server.Player
{
    /// <summary>
    ///     This class will manage connected player sessions.
    /// </summary>
    public class PlayerManager : IPlayerManager
    {
        [Dependency]
        private readonly IBaseServer _baseServer;

        [Dependency]
        private readonly IServerEntityManager _entityManager;

        [Dependency]
        private readonly IServerNetManager _network;

        /// <summary>
        ///     Number of active sessions.
        ///     This is the cached value of _sessions.Count(s => s != null);
        /// </summary>
        private int _sessionCount;

        /// <summary>
        ///     Active sessions of connected clients to the server.
        /// </summary>
        private PlayerSession[] _sessions;

        public string PlayerPrototypeName { get; set; } = "__engine_human";

        public LocalCoordinates FallbackSpawnPoint { get; set; }

        /// <inheritdoc />
        public int PlayerCount => _sessionCount;

        /// <inheritdoc />
        public int MaxPlayers => _sessions.Length;

        /// <inheritdoc />
        public event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        /// <inheritdoc />
        public void Initialize(int maxPlayers)
        {
            _sessions = new PlayerSession[maxPlayers];

            _network.RegisterNetMessage<MsgSession>(MsgSession.NAME);
            _network.RegisterNetMessage<MsgClGreet>(MsgClGreet.NAME, message => HandleClientGreet((MsgClGreet)message));
            _network.RegisterNetMessage<MsgServerInfoReq>(MsgServerInfoReq.NAME, HandleWelcomeMessageReq);
            _network.RegisterNetMessage<MsgServerInfo>(MsgServerInfo.NAME);
            _network.RegisterNetMessage<MsgPlayerListReq>(MsgPlayerListReq.NAME, HandlePlayerListReq);
            _network.RegisterNetMessage<MsgPlayerList>(MsgPlayerList.NAME);

            _network.Connecting += OnConnecting;
            _network.Connected += NewSession;
            _network.Disconnect += EndSession;
        }

        /// <summary>
        ///     Spawns the players entity.
        /// </summary>
        /// <param name="session"></param>
        public void SpawnPlayerMob(IPlayerSession session)
        {
            var entity = _entityManager.ForceSpawnEntityAt(PlayerPrototypeName, FallbackSpawnPoint);
            session.AttachToEntity(entity);
        }

        public IPlayerSession GetSessionByChannel(INetChannel client)
        {
            // Should only be one session per client. Returns that session, in theory.
            return _sessions.FirstOrDefault(s => s?.ConnectedClient == client);
        }

        /// <summary>
        ///     Returns the client session of the networkId.
        /// </summary>
        /// <param name="index">The id of the client.</param>
        /// <returns></returns>
        public IPlayerSession GetSessionById(PlayerIndex index)
        {
            Debug.Assert(0 <= index && index <= MaxPlayers);
            return _sessions[index];
        }

        public bool ValidSessionId(PlayerIndex index)
        {
            return _sessions[index] != null;
        }

        /// <summary>
        ///     Causes all sessions to switch from the lobby to the the game.
        /// </summary>
        public void SendJoinGameToAll()
        {
            foreach (var s in _sessions)
                s?.JoinGame();
        }

        /// <summary>
        ///     Causes all sessions to switch from the game to the lobby.
        /// </summary>
        public void SendJoinLobbyToAll()
        {
            foreach (var s in _sessions)
                s?.JoinLobby();
        }

        /// <summary>
        ///     Causes all sessions to detach from their entity.
        /// </summary>
        public void DetachAll()
        {
            foreach (var s in _sessions)
                s?.DetachFromEntity();
        }

        /// <summary>
        ///     Gets all players inside of a circle.
        /// </summary>
        /// <param name="position">Position of the circle in world-space.</param>
        /// <param name="range">Radius of the circle in world units.</param>
        /// <returns></returns>
        public List<IPlayerSession> GetPlayersInRange(LocalCoordinates position, int range)
        {
            //TODO: This needs to be moved to the PVS system.
            return
                _sessions.Where(x => x != null &&
                                     x.AttachedEntity != null &&
                                     position.InRange(x.AttachedEntity.GetComponent<ITransformComponent>().LocalPosition, range))
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
                _sessions.Where(
                        x => x != null && x.Status == SessionStatus.InLobby)
                    .Cast<IPlayerSession>()
                    .ToList();
        }

        /// <summary>
        ///     Gets all players in the server.
        /// </summary>
        /// <returns></returns>
        public List<IPlayerSession> GetAllPlayers()
        {
            return _sessions.Where(x => x!= null).Cast<IPlayerSession>().ToList();
        }

        /// <summary>
        ///     Gets all player states in the server.
        /// </summary>
        /// <returns></returns>
        public List<PlayerState> GetPlayerStates()
        {
            return _sessions
                .Where(s => s != null)
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
        public void NewSession(object sender, NetChannelArgs args)
        {
            var pos = GetFirstOpenIndex();

            if (pos == -1)
                throw new InvalidOperationException("NewSession was called, but there are no available slots!");

            var index = new PlayerIndex(pos);
            var session = new PlayerSession(this, args.Channel, index);

            session.PlayerStatusChanged += (obj, sessionArgs) => OnPlayerStatusChanged(session, sessionArgs.OldStatus, sessionArgs.NewStatus);

            Debug.Assert(_sessions[pos] == null);
            _sessionCount++;
            _sessions[pos] = session;
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
            Debug.Assert(_sessionCount > 0);
            Debug.Assert(0 <= session.Index && session.Index <= MaxPlayers);
            Debug.Assert(_sessions[session.Index] != null);
            Debug.Assert(_sessions[session.Index].ConnectedClient.ConnectionId == args.Channel.ConnectionId);

            //Detach the entity and (don't)delete it.
            session.OnDisconnect();
            _sessionCount--;
            _sessions[session.Index] = null;
        }

        private int GetFirstOpenIndex()
        {
            Debug.Assert(PlayerCount <= MaxPlayers);

            if (PlayerCount == MaxPlayers)
                return -1;

            for (var i = 0; i < _sessions.Length; i++)
                if (_sessions[i] == null)
                    return i;

            Debug.Assert(true, "Why was a slot not found? There should be one.");
            return -1;
        }

        private void HandleClientGreet(MsgClGreet msg)
        {
            var p = GetSessionByChannel(msg.MsgChannel);

            var fixedName = msg.PlyName.Trim();
            if (fixedName.Length < 3)
                fixedName = $"Player {p.Index}";

            p.SetName(fixedName);
        }

        private void HandleWelcomeMessageReq(NetMessage message)
        {
            var session = GetSessionByChannel(message.MsgChannel);
            session.Name = ((MsgServerInfoReq)message).PlayerName;

            var netMsg = message.MsgChannel.CreateNetMessage<MsgServerInfo>();

            netMsg.ServerName = _baseServer.ServerName;
            netMsg.ServerPort = _network.Port;
            netMsg.ServerWelcomeMessage = _baseServer.Motd;
            netMsg.ServerMaxPlayers = _baseServer.MaxPlayers;
            netMsg.ServerMapName = _baseServer.MapName;
            netMsg.GameMode = _baseServer.GameModeName;
            netMsg.ServerPlayerCount = PlayerCount;
            netMsg.PlayerIndex = session.Index;

            message.MsgChannel.SendMessage(netMsg);
        }

        private void HandlePlayerListReq(NetMessage message)
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
                    Index = client.Index,
                    Uuid = client.ConnectedClient.ConnectionId,
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
