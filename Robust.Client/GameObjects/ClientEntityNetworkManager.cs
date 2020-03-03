using System;
using System.Collections.Generic;
using Robust.Client.Interfaces.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// The client implementation of the Entity Network Manager.
    /// </summary>
    public class ClientEntityNetworkManager : IEntityNetworkManager
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _networkManager;
        [Dependency] private readonly IClientGameStateManager _gameStateManager;
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        /// <inheritdoc />
        public event EventHandler<NetworkComponentMessage> ReceivedComponentMessage;

        /// <inheritdoc />
        public event EventHandler<EntitySystemMessage> ReceivedSystemMessage;

        private readonly PriorityQueue<MsgEntity> _queue = new PriorityQueue<MsgEntity>(new MessageTickComparer());

        /// <inheritdoc />
        public void SetupNetworking()
        {
            _networkManager.RegisterNetMessage<MsgEntity>(MsgEntity.NAME, HandleEntityNetworkMessage);
        }

        public void Update()
        {
            while (_queue.Count != 0 && _queue.Peek().SourceTick <= _gameStateManager.CurServerTick)
            {
                DispatchMsgEntity(_queue.Take());
            }
        }

        /// <inheritdoc />
        public void SendSystemNetworkMessage(EntitySystemMessage message)
        {
            SendSystemNetworkMessage(message, default(uint));
        }

        public void SendSystemNetworkMessage(EntitySystemMessage message, uint sequence)
        {
            var msg = _networkManager.CreateNetMessage<MsgEntity>();
            msg.Type = EntityMessageType.SystemMessage;
            msg.SystemMessage = message;
            msg.SourceTick = _gameTiming.CurTick;
            msg.Sequence = sequence;

            _networkManager.ClientSendMessage(msg);
        }

        /// <inheritdoc />
        public void SendSystemNetworkMessage(EntitySystemMessage message, INetChannel channel)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void SendComponentNetworkMessage(INetChannel channel, IEntity entity, IComponent component, ComponentMessage message)
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

            _queue.Add(message);
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

        private sealed class MessageTickComparer : IComparer<MsgEntity>
        {
            public int Compare(MsgEntity x, MsgEntity y)
            {
                DebugTools.AssertNotNull(x);
                DebugTools.AssertNotNull(y);

                return y.SourceTick.CompareTo(x.SourceTick);
            }
        }
    }
}
