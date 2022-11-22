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
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public sealed class ClientEntityManager : EntityManager, IClientEntityManagerInternal
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClientNetManager _networkManager = default!;
        [Dependency] private readonly IClientGameTiming _gameTiming = default!;
        [Dependency] private readonly IClientGameStateManager _stateMan = default!;
        [Dependency] private readonly IBaseClient _client = default!;

        protected override int NextEntityUid { get; set; } = EntityUid.ClientUid + 1;

        public override void Initialize()
        {
            SetupNetworking();
            ReceivedSystemMessage += (_, systemMsg) => EventBus.RaiseEvent(EventSource.Network, systemMsg);

            base.Initialize();
        }
        public override void Shutdown()
        {
            using var _ = _gameTiming.StartStateApplicationArea();
            base.Shutdown();
        }

        public override void Cleanup()
        {
            using var _ = _gameTiming.StartStateApplicationArea();
            base.Cleanup();
        }

        EntityUid IClientEntityManagerInternal.CreateEntity(string? prototypeName, EntityUid uid)
        {
            return base.CreateEntity(prototypeName, uid);
        }

        void IClientEntityManagerInternal.InitializeEntity(EntityUid entity, MetaDataComponent? meta)
        {
            base.InitializeEntity(entity, meta);
        }

        void IClientEntityManagerInternal.StartEntity(EntityUid entity)
        {
            base.StartEntity(entity);
        }

        /// <inheritdoc />
        public override void DirtyEntity(EntityUid uid, MetaDataComponent? meta = null)
        {
            //  Client only dirties during prediction
            if (_gameTiming.InPrediction)
                base.DirtyEntity(uid, meta);
        }

        /// <inheritdoc />
        public override void Dirty(Component component, MetaDataComponent? meta = null)
        {
            //  Client only dirties during prediction
            if (_gameTiming.InPrediction)
                base.Dirty(component, meta);
        }

        public override EntityStringRepresentation ToPrettyString(EntityUid uid)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == uid)
                return base.ToPrettyString(uid) with { Session = _playerManager.LocalPlayer.Session };
            else
                return base.ToPrettyString(uid);
        }

        public override void RaisePredictiveEvent<T>(T msg)
        {
            var localPlayer = _playerManager.LocalPlayer;
            DebugTools.AssertNotNull(localPlayer);

            var sequence = _stateMan.SystemMessageDispatched(msg);
            EntityNetManager?.SendSystemNetworkMessage(msg, sequence);

            if (!_stateMan.IsPredictionEnabled)
                return;

            DebugTools.Assert(_gameTiming.InPrediction && _gameTiming.IsFirstTimePredicted || _client.RunLevel != ClientRunLevel.Connected);

            var eventArgs = new EntitySessionEventArgs(localPlayer!.Session);
            EventBus.RaiseEvent(EventSource.Local, msg);
            EventBus.RaiseEvent(EventSource.Local, new EntitySessionMessage<T>(eventArgs, msg));
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
        public void SendSystemNetworkMessage(EntityEventArgs message, INetChannel channel)
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
                    DispatchReceivedNetworkMsg(message.SystemMessage);
                    return;
            }
        }

        public void DispatchReceivedNetworkMsg(EntityEventArgs msg)
        {
            var sessionType = typeof(EntitySessionMessage<>).MakeGenericType(msg.GetType());
            var sessionMsg = Activator.CreateInstance(sessionType, new EntitySessionEventArgs(_playerManager.LocalPlayer!.Session), msg)!;
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
    }
}
