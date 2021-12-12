using System;
using System.Collections.Generic;
using Prometheus;
using Robust.Client.GameStates;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    internal class ClientEntityManager : EntityManager, IClientEntityManagerInternal
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClientNetManager _networkManager = default!;
        [Dependency] private readonly IClientGameStateManager _gameStateManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        protected override int NextEntityUid { get; set; } = EntityUid.ClientUid + 1;

        public override void Initialize()
        {
            SetupNetworking();
            ReceivedComponentMessage += (_, compMsg) => DispatchComponentMessage(compMsg);
            ReceivedSystemMessage += (_, systemMsg) => EventBus.RaiseEvent(EventSource.Network, systemMsg);

            base.Initialize();
        }

        EntityUid IClientEntityManagerInternal.CreateEntity(string? prototypeName, EntityUid uid)
        {
            return base.CreateEntity(prototypeName, uid);
        }

        void IClientEntityManagerInternal.InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);
        }

        void IClientEntityManagerInternal.StartEntity(EntityUid entity)
        {
            base.StartEntity(entity);
        }

        #region IEntityNetworkManager impl

        public override IEntityNetworkManager EntityNetManager => this;

        /// <inheritdoc />
        public event EventHandler<NetworkComponentMessage>? ReceivedComponentMessage;

        /// <inheritdoc />
        public event EventHandler<object>? ReceivedSystemMessage;

        private readonly PriorityQueue<(uint seq, MsgEntity msg)> _queue = new(new MessageTickComparer());
        private uint _incomingMsgSequence;

        /// <inheritdoc />
        public void SetupNetworking()
        {
            _networkManager.RegisterNetMessage<MsgEntity>(HandleEntityNetworkMessage);
        }

        public override void TickUpdate(float frameTime, Histogram? histogram)
        {
            using (histogram?.WithLabels("EntityNet").NewTimer())
            {
                while (_queue.Count != 0 && _queue.Peek().msg.SourceTick <= _gameStateManager.CurServerTick)
                {
                    var (_, msg) = _queue.Take();
                    // Logger.DebugS("net.ent", "Dispatching: {0}: {1}", seq, msg);
                    DispatchMsgEntity(msg);
                }
            }

            base.TickUpdate(frameTime, histogram);
        }

        /// <inheritdoc />
        public void SendSystemNetworkMessage(EntityEventArgs message)
        {
            SendSystemNetworkMessage(message, default(uint));
        }

        public void SendSystemNetworkMessage(EntityEventArgs message, uint sequence)
        {
            var msg = _networkManager.CreateNetMessage<MsgEntity>();
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

        /// <inheritdoc />
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public void SendComponentNetworkMessage(INetChannel? channel, EntityUid entity, IComponent component, ComponentMessage message)
        {
            var netId = ComponentFactory.GetRegistration(component.GetType()).NetID;

            if (!netId.HasValue)
                throw new ArgumentException($"Component {component.Name} does not have a NetID.", nameof(component));

            var msg = _networkManager.CreateNetMessage<MsgEntity>();
            msg.Type = EntityMessageType.ComponentMessage;
            msg.EntityUid = entity;
            msg.NetId = netId.Value;
            msg.ComponentMessage = message;
            msg.SourceTick = _gameTiming.CurTick;

            _networkManager.ClientSendMessage(msg);
        }

        private void HandleEntityNetworkMessage(MsgEntity message)
        {
            if (message.SourceTick <= _gameStateManager.CurServerTick)
            {
                DispatchMsgEntity(message);
                return;
            }

            // MsgEntity is sent with ReliableOrdered so Lidgren guarantees ordering of incoming messages.
            // We still need to store a sequence input number to ensure ordering remains consistent in
            // the priority queue.
            _queue.Add((++_incomingMsgSequence, message));
        }

        private void DispatchMsgEntity(MsgEntity message)
        {
            switch (message.Type)
            {
                case EntityMessageType.ComponentMessage:
                    ReceivedComponentMessage?.Invoke(this, new NetworkComponentMessage(message));
                    return;

                case EntityMessageType.SystemMessage:
                    var msg = message.SystemMessage;
                    var sessionType = typeof(EntitySessionMessage<>).MakeGenericType(msg.GetType());
                    var sessionMsg = Activator.CreateInstance(sessionType, new EntitySessionEventArgs(_playerManager.LocalPlayer!.Session), msg)!;
                    ReceivedSystemMessage?.Invoke(this, msg);
                    ReceivedSystemMessage?.Invoke(this, sessionMsg);
                    return;
            }
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
