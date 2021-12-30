using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Prometheus;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    [UsedImplicitly] // DI Container
    public sealed class ServerEntityManager : EntityManager, IServerEntityManagerInternal
    {
        private static readonly Gauge EntitiesCount = Metrics.CreateGauge(
            "robust_entities_count",
            "Amount of alive entities.");

        [Dependency] private readonly IServerNetManager _networkManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        protected override int NextEntityUid { get; set; } = (int) EntityUid.FirstUid;

        public override void Initialize()
        {
            SetupNetworking();
            ReceivedComponentMessage += (_, compMsg) => DispatchComponentMessage(compMsg);
            ReceivedSystemMessage += (_, systemMsg) => EventBus.RaiseEvent(EventSource.Network, systemMsg);

            base.Initialize();
        }

        EntityUid IServerEntityManagerInternal.AllocEntity(string? prototypeName, EntityUid uid)
        {
            return AllocEntity(prototypeName, uid);
        }

        void IServerEntityManagerInternal.FinishEntityLoad(EntityUid entity, IEntityLoadContext? context)
        {
            LoadEntity(entity, context);
        }

        void IServerEntityManagerInternal.FinishEntityInitialization(EntityUid entity)
        {
            InitializeEntity(entity);
        }

        void IServerEntityManagerInternal.FinishEntityStartup(EntityUid entity)
        {
            StartEntity(entity);
        }

        private protected override EntityUid CreateEntity(string? prototypeName, EntityUid uid = default)
        {
            var entity = base.CreateEntity(prototypeName, uid);

            if (!string.IsNullOrWhiteSpace(prototypeName))
            {
                var prototype = PrototypeManager.Index<EntityPrototype>(prototypeName);

                // At this point in time, all data configure on the entity *should* be purely from the prototype.
                // As such, we can reset the modified ticks to Zero,
                // which indicates "not different from client's own deserialization".
                // So the initial data for the component or even the creation doesn't have to be sent over the wire.
                foreach (var (netId, component) in GetNetComponents(entity))
                {
                    // Make sure to ONLY get components that are defined in the prototype.
                    // Others could be instantiated directly by AddComponent (e.g. ContainerManager).
                    // And those aren't guaranteed to exist on the client, so don't clear them.
                    if (prototype.Components.ContainsKey(component.Name)) ((Component) component).ClearTicks();
                }
            }

            return entity;
        }

        public override EntityStringRepresentation ToPrettyString(EntityUid uid)
        {
            TryGetComponent(uid, out ActorComponent? actor);

            return base.ToPrettyString(uid) with { Session = actor?.PlayerSession };
        }

        #region IEntityNetworkManager impl

        public override IEntityNetworkManager EntityNetManager => this;

        /// <inheritdoc />
        public event EventHandler<NetworkComponentMessage>? ReceivedComponentMessage;

        /// <inheritdoc />
        public event EventHandler<object>? ReceivedSystemMessage;

        private readonly PriorityQueue<MsgEntity> _queue = new(new MessageSequenceComparer());

        private readonly Dictionary<IPlayerSession, uint> _lastProcessedSequencesCmd =
            new();

        private readonly Dictionary<EntityUid, List<(GameTick tick, ushort netId)>> _componentDeletionHistory = new();

        private bool _logLateMsgs;

        /// <inheritdoc />
        public void SetupNetworking()
        {
            _networkManager.RegisterNetMessage<MsgEntity>(HandleEntityNetworkMessage);

            // For syncing component deletions.
            EntityDeleted += OnEntityRemoved;
            ComponentRemoved += OnComponentRemoved;

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

            _configurationManager.OnValueChanged(CVars.NetLogLateMsg, b => _logLateMsgs = b, true);
        }

        /// <inheritdoc />
        public override void TickUpdate(float frameTime, bool noPredictions, Histogram? histogram)
        {
            using (histogram?.WithLabels("EntityNet").NewTimer())
            {
                while (_queue.Count != 0 && _queue.Peek().SourceTick <= _gameTiming.CurTick)
                {
                    DispatchEntityNetworkMessage(_queue.Take());
                }
            }

            base.TickUpdate(frameTime, noPredictions, histogram);

            EntitiesCount.Set(Entities.Count);
        }

        public uint GetLastMessageSequence(IPlayerSession session)
        {
            return _lastProcessedSequencesCmd[session];
        }

        private void OnEntityRemoved(object? sender, EntityUid e)
        {
            if (_componentDeletionHistory.ContainsKey(e))
                _componentDeletionHistory.Remove(e);
        }

        private void OnComponentRemoved(object? sender, ComponentEventArgs e)
        {
            var reg = ComponentFactory.GetRegistration(e.Component.GetType());

            // We only keep track of networked components being removed.
            if (reg.NetID is not {} netId)
                return;

            var uid = e.Owner;

            if (!_componentDeletionHistory.TryGetValue(uid, out var list))
            {
                list = new List<(GameTick tick, ushort netId)>();
                _componentDeletionHistory[uid] = list;
            }

            list.Add((_gameTiming.CurTick, netId));
        }

        public List<ushort> GetDeletedComponents(EntityUid uid, GameTick fromTick)
        {
            // TODO: Maybe make this a struct enumerator? Right now it's a list for consistency...
            var list = new List<ushort>();

            if (!_componentDeletionHistory.TryGetValue(uid, out var history))
                return list;

            foreach (var (tick, id) in history)
            {
                if (tick >= fromTick) list.Add(id);
            }

            return list;
        }

        public void CullDeletionHistory(GameTick oldestAck)
        {
            var remQueue = new RemQueue<EntityUid>();

            foreach (var (uid, list) in _componentDeletionHistory)
            {
                list.RemoveAll(hist => hist.tick < oldestAck);

                if(list.Count == 0)
                    remQueue.Add(uid);
            }

            foreach (var uid in remQueue)
            {
                _componentDeletionHistory.Remove(uid);
            }
        }

        /// <inheritdoc />
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public void SendComponentNetworkMessage(INetChannel? channel, EntityUid entity, IComponent component,
            ComponentMessage message)
        {
            if (_networkManager.IsClient)
                return;

            var netId = ComponentFactory.GetRegistration(component.GetType()).NetID;

            if (!netId.HasValue)
                throw new ArgumentException($"Component {component.Name} does not have a NetID.", nameof(component));

            var msg = _networkManager.CreateNetMessage<MsgEntity>();
            msg.Type = EntityMessageType.ComponentMessage;
            msg.EntityUid = entity;
            msg.NetId = netId.Value;
            msg.ComponentMessage = message;
            msg.SourceTick = _gameTiming.CurTick;

            // Logger.DebugS("net.ent", "Sending: {0}", msg);

            //Send the message
            if (channel == null)
                _networkManager.ServerSendToAll(msg);
            else
                _networkManager.ServerSendMessage(msg, channel);
        }

        /// <inheritdoc />
        public void SendSystemNetworkMessage(EntityEventArgs message)
        {
            var newMsg = _networkManager.CreateNetMessage<MsgEntity>();
            newMsg.Type = EntityMessageType.SystemMessage;
            newMsg.SystemMessage = message;
            newMsg.SourceTick = _gameTiming.CurTick;

            _networkManager.ServerSendToAll(newMsg);
        }

        /// <inheritdoc />
        public void SendSystemNetworkMessage(EntityEventArgs message, INetChannel targetConnection)
        {
            var newMsg = _networkManager.CreateNetMessage<MsgEntity>();
            newMsg.Type = EntityMessageType.SystemMessage;
            newMsg.SystemMessage = message;
            newMsg.SourceTick = _gameTiming.CurTick;

            _networkManager.ServerSendMessage(newMsg, targetConnection);
        }

        private void HandleEntityNetworkMessage(MsgEntity message)
        {
            var msgT = message.SourceTick;
            var cT = _gameTiming.CurTick;

            if (msgT <= cT)
            {
                if (msgT < cT && _logLateMsgs)
                {
                    Logger.WarningS("net.ent", "Got late MsgEntity! Diff: {0}, msgT: {2}, cT: {3}, player: {1}",
                        (int) msgT.Value - (int) cT.Value, message.MsgChannel.UserName, msgT, cT);
                }

                DispatchEntityNetworkMessage(message);
                return;
            }

            _queue.Add(message);
        }

        private void DispatchEntityNetworkMessage(MsgEntity message)
        {
            // Don't try to retrieve the session if the client disconnected
            if (!message.MsgChannel.IsConnected)
            {
                return;
            }

            var player = _playerManager.GetSessionByChannel(message.MsgChannel);

            if (message.Sequence != 0)
            {
                if (_lastProcessedSequencesCmd[player] < message.Sequence)
                {
                    _lastProcessedSequencesCmd[player] = message.Sequence;
                }
            }

#if EXCEPTION_TOLERANCE
            try
#endif
            {
                switch (message.Type)
                {
                    case EntityMessageType.ComponentMessage:
                        ReceivedComponentMessage?.Invoke(this, new NetworkComponentMessage(message, player));
                        return;

                    case EntityMessageType.SystemMessage:
                        var msg = message.SystemMessage;
                        var sessionType = typeof(EntitySessionMessage<>).MakeGenericType(msg.GetType());
                        var sessionMsg =
                            Activator.CreateInstance(sessionType, new EntitySessionEventArgs(player), msg)!;
                        ReceivedSystemMessage?.Invoke(this, msg);
                        ReceivedSystemMessage?.Invoke(this, sessionMsg);
                        return;
                }
            }
#if EXCEPTION_TOLERANCE
            catch (Exception e)
            {
                Logger.ErrorS("net.ent", $"Caught exception while dispatching {message.Type}: {e}");
            }
#endif
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            switch (args.NewStatus)
            {
                case SessionStatus.Connected:
                    _lastProcessedSequencesCmd.Add(args.Session, 0);
                    break;

                case SessionStatus.Disconnected:
                    _lastProcessedSequencesCmd.Remove(args.Session);
                    break;
            }
        }

        internal sealed class MessageSequenceComparer : IComparer<MsgEntity>
        {
            public int Compare(MsgEntity? x, MsgEntity? y)
            {
                DebugTools.AssertNotNull(x);
                DebugTools.AssertNotNull(y);

                var cmp = y!.SourceTick.CompareTo(x!.SourceTick);
                if (cmp != 0)
                {
                    return cmp;
                }

                return y.Sequence.CompareTo(x.Sequence);
            }
        }

        #endregion
    }
}
