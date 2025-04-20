using System;
using System.Collections.Generic;
using Prometheus;
using Robust.Client.GameStates;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public sealed partial class ClientEntityManager : EntityManager, IClientEntityManagerInternal
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClientNetManager _networkManager = default!;
        [Dependency] private readonly IClientGameTiming _gameTiming = default!;
        [Dependency] private readonly IClientGameStateManager _stateMan = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;

        internal event Action? AfterStartup;
        internal event Action? AfterShutdown;

        public override void Initialize()
        {
            SetupNetworking();
            ReceivedSystemMessage += (_, systemMsg) => EventBus.RaiseEvent(EventSource.Network, systemMsg);

            base.Initialize();
        }

        public override void Startup()
        {
            base.Startup();

            AfterStartup?.Invoke();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            AfterShutdown?.Invoke();
        }

        public override void FlushEntities()
        {
            // Server doesn't network deletions on client shutdown so we need to
            // manually clear these out or risk stale data getting used.
            PendingNetEntityStates.Clear();
            using var _ = _gameTiming.StartStateApplicationArea();
            base.FlushEntities();
        }

        EntityUid IClientEntityManagerInternal.CreateEntity(string? prototypeName, out MetaDataComponent metadata)
        {
            return base.CreateEntity(prototypeName, out metadata);
        }

        /// <inheritdoc />
        public override void DirtyEntity(EntityUid uid, MetaDataComponent? meta = null)
        {
            //  Client only dirties during prediction
            if (_gameTiming.InPrediction)
                base.DirtyEntity(uid, meta);
        }

        public override void QueueDeleteEntity(EntityUid? uid)
        {
            if (uid == null || uid == EntityUid.Invalid)
                return;

            if (IsClientSide(uid.Value))
            {
                base.QueueDeleteEntity(uid);
                return;
            }

            if (ShuttingDown)
                return;

            // Client-side entity deletion is not supported and will cause errors.
            if (_client.RunLevel == ClientRunLevel.Connected || _client.RunLevel == ClientRunLevel.InGame)
                LogManager.RootSawmill.Error($"Predicting the queued deletion of a networked entity: {ToPrettyString(uid.Value)}. Trace: {Environment.StackTrace}");
        }

        /// <inheritdoc />
        public override void Dirty(EntityUid uid, IComponent component, MetaDataComponent? meta = null)
        {
            Dirty(new Entity<IComponent>(uid, component), meta);
        }

        /// <inheritdoc />
        public override void Dirty<T>(Entity<T> ent, MetaDataComponent? meta = null)
        {
            // Client only dirties during prediction
            if (_gameTiming.InPrediction)
                base.Dirty(ent, meta);
        }

        public override void DirtyField<T>(EntityUid uid, T comp, string fieldName, MetaDataComponent? metadata = null)
        {
            // TODO Prediction
            // does the client actually need to dirty the field?
            // I.e., can't it just dirty the whole component to trigger a reset?

            // Client only dirties during prediction
            if (_gameTiming.InPrediction)
                base.DirtyField(uid, comp, fieldName, metadata);
        }

        public override void DirtyFields<T>(EntityUid uid, T comp, MetaDataComponent? meta, params string[] fields)
        {
            // TODO Prediction
            // does the client actually need to dirty the field?
            // I.e., can't it just dirty the whole component to trigger a reset?

            // Client only dirties during prediction
            if (_gameTiming.InPrediction)
                base.DirtyFields(uid, comp, meta, fields);
        }

        /// <inheritdoc />
        public override void Dirty<T1, T2>(Entity<T1, T2> ent, MetaDataComponent? meta = null)
        {
            if (_gameTiming.InPrediction)
                base.Dirty(ent, meta);
        }

        /// <inheritdoc />
        public override void Dirty<T1, T2, T3>(Entity<T1, T2, T3> ent, MetaDataComponent? meta = null)
        {
            if (_gameTiming.InPrediction)
                base.Dirty(ent, meta);
        }

        /// <inheritdoc />
        public override void Dirty<T1, T2, T3, T4>(Entity<T1, T2, T3, T4> ent, MetaDataComponent? meta = null)
        {
            if (_gameTiming.InPrediction)
                base.Dirty(ent, meta);
        }

        public override void RaisePredictiveEvent<T>(T msg)
        {
            var session = _playerManager.LocalSession;
            DebugTools.AssertNotNull(session);

            var sequence = _stateMan.SystemMessageDispatched(msg);
            EntityNetManager?.SendSystemNetworkMessage(msg, sequence);

            if (!_stateMan.IsPredictionEnabled && _client.RunLevel != ClientRunLevel.SinglePlayerGame)
                return;

            DebugTools.Assert(_gameTiming.InPrediction && _gameTiming.IsFirstTimePredicted || _client.RunLevel == ClientRunLevel.SinglePlayerGame);

            var eventArgs = new EntitySessionEventArgs(session!);
            EventBus.RaiseEvent(EventSource.Local, msg);
            EventBus.RaiseEvent(EventSource.Local, new EntitySessionMessage<T>(eventArgs, msg));
        }

        /// <inheritdoc />
        public override void RaiseSharedEvent<T>(T message, EntityUid? user = null)
        {
            if (user == null || user != _playerManager.LocalEntity || !_gameTiming.IsFirstTimePredicted)
                return;

            EventBus.RaiseEvent(EventSource.Local, ref message);
        }

        /// <inheritdoc />
        public override void RaiseSharedEvent<T>(T message, ICommonSession? user = null)
        {
            if (user == null || user != _playerManager.LocalSession || !_gameTiming.IsFirstTimePredicted)
                return;

            EventBus.RaiseEvent(EventSource.Local, ref message);
        }

        #region IEntityNetworkManager impl

        public override IEntityNetworkManager EntityNetManager => this;

        /// <inheritdoc />
        public event EventHandler<object>? ReceivedSystemMessage;

        private readonly PriorityQueue<(uint seq, MsgEntity msg)> _queue = new(new MessageTickComparer());
        private uint _incomingMsgSequence = 0;

        /// <inheritdoc />
        public void SetupNetworking()
        {
            _networkManager.RegisterNetMessage<MsgEntity>(HandleEntityNetworkMessage);
        }

        public override void TickUpdate(float frameTime, bool noPredictions, Histogram? histogram)
        {
            using (histogram?.WithLabels("EntityNet").NewTimer())
            {
                while (_queue.Count != 0 && _queue.Peek().msg.SourceTick <= _gameTiming.LastRealTick)
                {
                    var (_, msg) = _queue.Take();
                    // Logger.DebugS("net.ent", "Dispatching: {0}: {1}", seq, msg);
                    DispatchReceivedNetworkMsg(msg);
                }
            }

            base.TickUpdate(frameTime, noPredictions, histogram);
        }

        /// <inheritdoc />
        public void SendSystemNetworkMessage(EntityEventArgs message, bool recordReplay = true)
        {
            SendSystemNetworkMessage(message, default(uint));
        }

        public void SendSystemNetworkMessage(EntityEventArgs message, uint sequence)
        {
            var msg = new MsgEntity();
            msg.Type = EntityMessageType.SystemMessage;
            msg.SystemMessage = message;
            msg.SourceTick = _gameTiming.CurTick;
            msg.Sequence = sequence;

            _networkManager.ClientSendMessage(msg);
        }

        /// <inheritdoc />
        public void SendSystemNetworkMessage(EntityEventArgs message, INetChannel? channel)
        {
            throw new NotSupportedException();
        }

        private void HandleEntityNetworkMessage(MsgEntity message)
        {
            if (message.SourceTick <= _gameTiming.LastRealTick)
            {
                DispatchReceivedNetworkMsg(message);
                return;
            }

            // MsgEntity is sent with ReliableOrdered so Lidgren guarantees ordering of incoming messages.
            // We still need to store a sequence input number to ensure ordering remains consistent in
            // the priority queue.
            _queue.Add((++_incomingMsgSequence, message));
        }

        private void DispatchReceivedNetworkMsg(MsgEntity message)
        {
            switch (message.Type)
            {
                case EntityMessageType.SystemMessage:

                    // TODO REPLAYS handle late messages.
                    // If a message was received late, it will be recorded late here.
                    // Maybe process the replay to prevent late messages when playing back?
                    _replayRecording.RecordReplayMessage(message.SystemMessage);

                    DispatchReceivedNetworkMsg(message.SystemMessage);
                    return;
            }
        }

        public void DispatchReceivedNetworkMsg(EntityEventArgs msg)
        {
            var sessionType = typeof(EntitySessionMessage<>).MakeGenericType(msg.GetType());
            var sessionMsg = Activator.CreateInstance(sessionType, new EntitySessionEventArgs(_playerManager.LocalSession!), msg)!;
            ReceivedSystemMessage?.Invoke(this, msg);
            ReceivedSystemMessage?.Invoke(this, sessionMsg);
        }

        private sealed class MessageTickComparer : IComparer<(uint seq, MsgEntity msg)>
        {
            public int Compare((uint seq, MsgEntity msg) x, (uint seq, MsgEntity msg) y)
            {
                var cmp = y.msg.SourceTick.CompareTo(x.msg.SourceTick);
                if (cmp != 0)
                {
                    return cmp;
                }

                return y.seq.CompareTo(x.seq);
            }
        }
        #endregion

        /// <inheritdoc />
        public override void PredictedDeleteEntity(Entity<MetaDataComponent?, TransformComponent?> ent)
        {
            if (!MetaQuery.Resolve(ent.Owner, ref ent.Comp1)
                || ent.Comp1.EntityDeleted
                || !TransformQuery.Resolve(ent.Owner, ref ent.Comp2))
            {
                return;
            }

            // So there's 3 scenarios:
            // 1. Networked entity we just move to nullspace and rely on state handling.
            // 2. Clientside predicted entity we delete and rely on state handling.
            // 3. Clientside only entity that actually needs deleting here.

            if (ent.Comp1.NetEntity.IsClientSide())
            {
                DeleteEntity(ent, ent.Comp1, ent.Comp2);
            }
            else
            {
                _xforms.DetachEntity(ent, ent.Comp2);
            }
        }

        /// <inheritdoc />
        public override void PredictedQueueDeleteEntity(Entity<MetaDataComponent?, TransformComponent?> ent)
        {
            if (IsQueuedForDeletion(ent.Owner)
                || !MetaQuery.Resolve(ent.Owner, ref ent.Comp1)
                || ent.Comp1.EntityDeleted
                || !TransformQuery.Resolve(ent.Owner, ref ent.Comp2))
            {
                return;
            }

            if (ent.Comp1.NetEntity.IsClientSide())
            {
                // client-side QueueDeleteEntity re-fetches MetadataComp and checks IsClientSide().
                // base call to skip that.
                // TODO create override that takes in metadata comp
                base.QueueDeleteEntity(ent);
            }
            else
            {
                _xforms.DetachEntity(ent.Owner, ent.Comp2);
            }
        }
    }
}
