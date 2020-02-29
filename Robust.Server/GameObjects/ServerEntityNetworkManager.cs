using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;

namespace Robust.Server.GameObjects
{
    /// <summary>
    /// The server implementation of the Entity Network Manager.
    /// </summary>
    public class ServerEntityNetworkManager : IEntityNetworkManager
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNetManager _networkManager;
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        /// <inheritdoc />
        public event EventHandler<NetworkComponentMessage> ReceivedComponentMessage;

        /// <inheritdoc />
        public event EventHandler<EntitySystemMessage> ReceivedSystemMessage;

        /// <inheritdoc />
        public void SetupNetworking()
        {
            _networkManager.RegisterNetMessage<MsgEntity>(MsgEntity.NAME, HandleEntityNetworkMessage);
        }

        /// <inheritdoc />
        public void SendComponentNetworkMessage(INetChannel channel, IEntity entity, IComponent component, ComponentMessage message)
        {
            if (_networkManager.IsClient)
                return;

            if (!component.NetID.HasValue)
                throw new ArgumentException($"Component {component.Name} does not have a NetID.", nameof(component));

            var msg = _networkManager.CreateNetMessage<MsgEntity>();
            msg.Type = EntityMessageType.ComponentMessage;
            msg.EntityUid = entity.Uid;
            msg.NetId = component.NetID.Value;
            msg.ComponentMessage = message;
            msg.SourceTick = _gameTiming.CurTick;

            //Send the message
            if (channel == null)
                _networkManager.ServerSendToAll(msg);
            else
                _networkManager.ServerSendMessage(msg, channel);
        }

        /// <inheritdoc />
        public void SendSystemNetworkMessage(EntitySystemMessage message)
        {
            var newMsg = _networkManager.CreateNetMessage<MsgEntity>();
            newMsg.Type = EntityMessageType.SystemMessage;
            newMsg.SystemMessage = message;
            newMsg.SourceTick = _gameTiming.CurTick;

            _networkManager.ServerSendToAll(newMsg);
        }

        /// <inheritdoc />
        public void SendSystemNetworkMessage(EntitySystemMessage message, INetChannel targetConnection)
        {
            var newMsg = _networkManager.CreateNetMessage<MsgEntity>();
            newMsg.Type = EntityMessageType.SystemMessage;
            newMsg.SystemMessage = message;
            newMsg.SourceTick = _gameTiming.CurTick;

            _networkManager.ServerSendMessage(newMsg, targetConnection);
        }

        private void HandleEntityNetworkMessage(MsgEntity message)
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
    }
}
