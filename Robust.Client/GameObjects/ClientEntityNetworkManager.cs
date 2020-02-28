using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// The client implementation of the Entity Network Manager.
    /// </summary>
    public class ClientEntityNetworkManager : IEntityNetworkManager
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _networkManager;
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
        public void SendSystemNetworkMessage(EntitySystemMessage message)
        {
            var msg = _networkManager.CreateNetMessage<MsgEntity>();
            msg.Type = EntityMessageType.SystemMessage;
            msg.SystemMessage = message;
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
            _networkManager.ClientSendMessage(msg);
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
