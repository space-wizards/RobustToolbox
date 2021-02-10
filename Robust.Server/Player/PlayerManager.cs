using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prometheus;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Server.Player
{
    /// <summary>
    ///     This class will manage connected player sessions.
    /// </summary>
    internal class PlayerManager : IPlayerManager
    {
        private static readonly Gauge PlayerCountMetric = Metrics
            .CreateGauge("robust_player_count", "Number of players on the server.");

        [Dependency] private readonly IBaseServer _baseServer = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IServerNetManager _network = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public BoundKeyMap KeyMap { get; private set; } = default!;

        private GameTick _lastStateUpdate;

        private readonly ReaderWriterLockSlim _sessionsLock = new();

        /// <summary>
        ///     Active sessions of connected clients to the server.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<NetUserId, PlayerSession> _sessions = new();

        [ViewVariables]
        private readonly Dictionary<NetUserId, PlayerData> _playerData = new();

        [ViewVariables]
        private readonly Dictionary<string, NetUserId> _userIdMap = new();


        /// <inheritdoc />
        [ViewVariables]
        public int PlayerCount
        {
            get
            {
                _sessionsLock.EnterReadLock();
                try
                {
                    return _sessions.Count;
                }
                finally
                {
                    _sessionsLock.ExitReadLock();
                }
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public int MaxPlayers { get; private set; } = 32;

        /// <inheritdoc />
        public event EventHandler<SessionStatusEventArgs>? PlayerStatusChanged;

        /// <inheritdoc />
        public void Initialize(int maxPlayers)
        {
            KeyMap = new BoundKeyMap(_reflectionManager);
            KeyMap.PopulateKeyFunctionsMap();

            MaxPlayers = maxPlayers;

            _network.RegisterNetMessage<MsgPlayerListReq>(MsgPlayerListReq.NAME, HandlePlayerListReq);
            _network.RegisterNetMessage<MsgPlayerList>(MsgPlayerList.NAME);

            _network.Connecting += OnConnecting;
            _network.Connected += NewSession;
            _network.Disconnect += EndSession;
        }

        public bool TryGetSessionByUsername(string username, [NotNullWhen(true)] out IPlayerSession? session)
        {
            if (!_userIdMap.TryGetValue(username, out var userId))
            {
                session = null;
                return false;
            }

            if (_sessions.TryGetValue(userId, out var iSession))
            {
                session = iSession;
                return true;
            }

            session = null;
            return false;
        }

        IPlayerSession IPlayerManager.GetSessionByChannel(INetChannel channel) => GetSessionByChannel(channel);
        public bool TryGetSessionByChannel(INetChannel channel, [NotNullWhen(true)] out IPlayerSession? session)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                // Should only be one session per client. Returns that session, in theory.
                if (_sessions.TryGetValue(channel.UserId, out var concrete))
                {
                    session = concrete;
                    return true;
                }

                session = null;
                return false;
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        private PlayerSession GetSessionByChannel(INetChannel channel)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                // Should only be one session per client. Returns that session, in theory.
                return _sessions[channel.UserId];
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public IPlayerSession GetSessionByUserId(NetUserId index)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                return _sessions[index];
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        public bool ValidSessionId(NetUserId index)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                return _sessions.ContainsKey(index);
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        public bool TryGetSessionById(NetUserId userId, [NotNullWhen(true)] out IPlayerSession? session)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                if (_sessions.TryGetValue(userId, out var _session))
                {
                    session = _session;
                    return true;
                }
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
            session = default;
            return false;
        }

        /// <summary>
        ///     Causes all sessions to switch from the lobby to the the game.
        /// </summary>
        public void SendJoinGameToAll()
        {
            _sessionsLock.EnterReadLock();
            try
            {
                foreach (var s in _sessions.Values)
                    s.JoinGame();
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        public bool TryGetUserId(string userName, out NetUserId userId)
        {
            return _userIdMap.TryGetValue(userName, out userId);
        }

        public IEnumerable<IPlayerData> GetAllPlayerData()
        {
            return _playerData.Values;
        }

        /// <summary>
        ///     Causes all sessions to detach from their entity.
        /// </summary>
        public void DetachAll()
        {
            _sessionsLock.EnterReadLock();
            try
            {
                foreach (var s in _sessions.Values)
                    s.DetachFromEntity();
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     Gets all players inside of a circle.
        /// </summary>
        /// <param name="worldPos">Position of the circle in world-space.</param>
        /// <param name="range">Radius of the circle in world units.</param>
        /// <returns></returns>
        public List<IPlayerSession> GetPlayersInRange(MapCoordinates worldPos, int range)
        {
            _sessionsLock.EnterReadLock();
            //TODO: This needs to be moved to the PVS system.
            try
            {
                return
                    _sessions.Values.Where(x =>
                            x.AttachedEntity != null && worldPos.InRange(x.AttachedEntity.Transform.MapPosition, range))
                        .Cast<IPlayerSession>()
                        .ToList();
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     Gets all players inside of a circle.
        /// </summary>
        /// <param name="worldPos">Position of the circle in world-space.</param>
        /// <param name="range">Radius of the circle in world units.</param>
        /// <returns></returns>
        public List<IPlayerSession> GetPlayersInRange(EntityCoordinates worldPos, int range)
        {
            return GetPlayersInRange(worldPos.ToMap(_entityManager), range);
        }

        public List<IPlayerSession> GetPlayersBy(Func<IPlayerSession, bool> predicate)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                return
                    _sessions.Values.Where(predicate)
                        .Cast<IPlayerSession>()
                        .ToList();
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     Gets all players in the server.
        /// </summary>
        /// <returns></returns>
        public List<IPlayerSession> GetAllPlayers()
        {
            _sessionsLock.EnterReadLock();
            try
            {
                return _sessions.Values.Cast<IPlayerSession>().ToList();
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     Gets all player states in the server.
        /// </summary>
        /// <param name="fromTick"></param>
        /// <returns></returns>
        public List<PlayerState>? GetPlayerStates(GameTick fromTick)
        {
            if (_lastStateUpdate < fromTick)
            {
                return null;
            }

            _sessionsLock.EnterReadLock();
            try
            {
                return _sessions.Values
                    .Select(s => s.PlayerState)
                    .ToList();
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        private Task OnConnecting(NetConnectingArgs args)
        {
            if (PlayerCount >= _baseServer.MaxPlayers)
            {
                args.Deny("The server is full.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Creates a new session for a client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void NewSession(object? sender, NetChannelArgs args)
        {
            if (!_playerData.TryGetValue(args.Channel.UserId, out var data))
            {
                data = new PlayerData(args.Channel.UserId);
                _playerData.Add(args.Channel.UserId, data);
            }

            _userIdMap[args.Channel.UserName] = args.Channel.UserId;

            var session = new PlayerSession(this, args.Channel, data);

            session.PlayerStatusChanged += (obj, sessionArgs) => OnPlayerStatusChanged(session, sessionArgs.OldStatus, sessionArgs.NewStatus);

            _sessionsLock.EnterWriteLock();
            try
            {
                _sessions.Add(args.Channel.UserId, session);
            }
            finally
            {
                _sessionsLock.ExitWriteLock();
            }

            PlayerCountMetric.Set(PlayerCount);

            IoCManager.Resolve<INetConfigurationManager>().SyncConnectingClient(args.Channel);
        }

        private void OnPlayerStatusChanged(IPlayerSession session, SessionStatus oldStatus, SessionStatus newStatus)
        {
            PlayerStatusChanged?.Invoke(this, new SessionStatusEventArgs(session, oldStatus, newStatus));
        }

        /// <summary>
        ///     Ends a clients session, and disconnects them.
        /// </summary>
        private void EndSession(object? sender, NetChannelArgs args)
        {
            if (!TryGetSessionByChannel(args.Channel, out var session))
            {
                return;
            }

            // make sure nothing got messed up during the life of the session
            DebugTools.Assert(session.ConnectedClient == args.Channel);

            //Detach the entity and (don't)delete it.
            session.OnDisconnect();
            _sessionsLock.EnterWriteLock();
            try
            {
                _sessions.Remove(session.UserId);
            }
            finally
            {
                _sessionsLock.ExitWriteLock();
            }

            PlayerCountMetric.Set(PlayerCount);
            Dirty();
        }

        private void HandlePlayerListReq(MsgPlayerListReq message)
        {
            var channel = message.MsgChannel;
            var players = GetAllPlayers().ToArray();
            var netMsg = channel.CreateNetMessage<MsgPlayerList>();

            // client session is complete, set their status accordingly.
            // This is done before the packet is built, so that the client
            // can see themselves Connected.
            var session = GetSessionByChannel(channel);
            session.Status = SessionStatus.Connected;

            var list = new List<PlayerState>();
            foreach (var client in players)
            {
                if (client == null)
                    continue;

                var info = new PlayerState
                {
                    UserId = client.UserId,
                    Name = client.Name,
                    Status = client.Status,
                    Ping = client.ConnectedClient.Ping
                };
                list.Add(info);
            }
            netMsg.Plyrs = list;
            netMsg.PlyCount = (byte)list.Count;

            channel.SendMessage(netMsg);
        }

        public void Dirty()
        {
            _lastStateUpdate = _timing.CurTick;
        }

        public IPlayerData GetPlayerData(NetUserId userId)
        {
            return _playerData[userId];
        }

        public bool TryGetPlayerData(NetUserId userId, [NotNullWhen(true)] out IPlayerData? data)
        {
            if (_playerData.TryGetValue(userId, out var _data))
            {
                data = _data;
                return true;
            }
            data = default;
            return false;
        }

        public bool TryGetPlayerDataByUsername(string userName, [NotNullWhen(true)] out IPlayerData? data)
        {
            if (!_userIdMap.TryGetValue(userName, out var userId))
            {
                data = null;
                return false;
            }

            // PlayerData is initialized together with the _userIdMap so we can trust that it'll be present.
            data = _playerData[userId];
            return true;
        }

        public bool HasPlayerData(NetUserId userId)
        {
            return _playerData.ContainsKey(userId);
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
