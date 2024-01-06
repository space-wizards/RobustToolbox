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
            DebugTools.Assert(session.Channel == args.Channel);

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
                    Status = client.Status,
                    Ping = client.Channel!.Ping
                };
                list.Add(info);
            }
            netMsg.Plyrs = list;
            netMsg.PlyCount = (byte)list.Count;

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
    }
}
