using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Prometheus;
using Robust.Server.Configuration;
using Robust.Server.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.Player
{
    /// <summary>
    ///     This class will manage connected player sessions.
    /// </summary>
    internal sealed class PlayerManager : SharedPlayerManager, IPlayerManager
    {
        private static readonly Gauge PlayerCountMetric = Metrics
            .CreateGauge("robust_player_count", "Number of players on the server.");

        [Dependency] private readonly IBaseServer _baseServer = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IServerNetManager _network = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IServerNetConfigurationManager _cfg = default!;

        public BoundKeyMap KeyMap { get; private set; } = default!;

        /// <inheritdoc />
        public override void Initialize(int maxPlayers)
        {
            base.Initialize(maxPlayers);
            KeyMap = new BoundKeyMap(_reflectionManager);
            KeyMap.PopulateKeyFunctionsMap();

            _network.RegisterNetMessage<MsgPlayerListReq>(HandlePlayerListReq);
            _network.RegisterNetMessage<MsgPlayerList>();
            _network.RegisterNetMessage<MsgSyncTimeBase>();

            _network.Connecting += OnConnecting;
            _network.Connected += NewSession;
            _network.Disconnect += EndSession;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            KeyMap = default!;

            _network.Connecting -= OnConnecting;
            _network.Connected -= NewSession;
            _network.Disconnect -= EndSession;
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
            CreateAndAddSession(args.Channel);
            PlayerCountMetric.Set(PlayerCount);
            // Synchronize base time.
            var msgTimeBase = new MsgSyncTimeBase();
            (msgTimeBase.Time, msgTimeBase.Tick) = _timing.TimeBase;
            _network.ServerSendMessage(msgTimeBase, args.Channel);

            _cfg.SyncConnectingClient(args.Channel);
        }

        private void EndSession(object? sender, NetChannelArgs args)
        {
            EndSession(args.Channel.UserId);
        }

        /// <summary>
        ///     Ends a clients session, and disconnects them.
        /// </summary>
        internal void EndSession(NetUserId user)
        {
            if (!TryGetSessionById(user, out var session))
                return;

            SetStatus(session, SessionStatus.Disconnected);
            SetAttachedEntity(session, null, out _, true);

            var viewSys = EntManager.System<ViewSubscriberSystem>();
            foreach (var eye in session.ViewSubscriptions.ToArray())
            {
                viewSys.RemoveViewSubscriber(eye, session);
            }

            RemoveSession(session.UserId);
            PlayerCountMetric.Set(PlayerCount);
            Dirty();
        }

        private void HandlePlayerListReq(MsgPlayerListReq message)
        {
            var channel = message.MsgChannel;
            var players = Sessions;
            var netMsg = new MsgPlayerList();

            // client session is complete, set their status accordingly.
            // This is done before the packet is built, so that the client
            // can see themselves Connected.
            var session = GetSessionByChannel(channel);
            session.ConnectedTime = DateTime.UtcNow;
            SetStatus(session, SessionStatus.Connected);

            var list = new List<SessionState>();
            foreach (var client in players)
            {
                var info = new SessionState
                {
                    UserId = client.UserId,
                    Name = client.Name,
                    Status = client.Status
                };
                list.Add(info);
            }
            netMsg.Plyrs = list;

            channel.SendMessage(netMsg);
        }

        public override bool TryGetSessionByEntity(EntityUid uid, [NotNullWhen(true)] out ICommonSession? session)
        {
            if (!_entityManager.TryGetComponent(uid, out ActorComponent? actor))
            {
                session = null;
                return false;
            }

            session = actor.PlayerSession;
            return true;
        }

    internal ICommonSession AddDummySession(NetUserId user, string name)
    {
#if FULL_RELEASE
        // Lets not make it completely trivial to fake player counts.
        throw new NotSupportedException();
#endif
        Lock.EnterWriteLock();
        DummySession session;
        try
        {
            UserIdMap[name] = user;
            if (!PlayerData.TryGetValue(user, out var data))
                PlayerData[user] = data = new(user, name);

            session = new DummySession(user, name, data);
            InternalSessions.Add(user, session);
        }
        finally
        {
            Lock.ExitWriteLock();
        }

        UpdateState(session);

        return session;
    }
    }
}
