using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Prometheus;
using Robust.Client.GameStates;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public sealed class ClientEntityManager : EntityManager, IClientEntityManagerInternal
    {
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

        IEntity IClientEntityManagerInternal.CreateEntity(string? prototypeName, EntityUid? uid)
        {
            return base.CreateEntity(prototypeName, uid);
        }

        void IClientEntityManagerInternal.InitializeEntity(IEntity entity)
        {
            EntityManager.InitializeEntity((Entity)entity);
        }

        void IClientEntityManagerInternal.StartEntity(IEntity entity)
        {
            base.StartEntity((Entity)entity);
        }

        #region IEntityNetworkManager impl

        public override IEntityNetworkManager EntityNetManager => this;

        /// <inheritdoc />
        public event EventHandler<NetworkComponentMessage>? ReceivedComponentMessage;

        /// <inheritdoc />
        public event EventHandler<object>? ReceivedSystemMessage;

        private readonly PriorityQueue<(uint seq, MsgEntity msg)> _queue = new(new MessageTickComparer());
        private uint _incomingMsgSequence = 0;

        /// <inheritdoc />
        public void SetupNetworking()
        {
            _networkManager.RegisterNetMessage<MsgEntity>(MsgEntity.NAME, HandleEntityNetworkMessage);
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
        public void SendComponentNetworkMessage(INetChannel? channel, IEntity entity, IComponent component, ComponentMessage message)
        {
            if (!component.NetID.HasValue)
                throw new ArgumentException($"Component {component.Name} does not have a NetID.", nameof(component));

            var msg = _networkManager.CreateNetMessage<MsgEntity>();
            msg.Type = EntityMessageType.ComponentMessage;
            msg.EntityUid = entity.Uid;
            msg.NetId = component.NetID.Value;
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
                    ReceivedSystemMessage?.Invoke(this, message.SystemMessage);
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
