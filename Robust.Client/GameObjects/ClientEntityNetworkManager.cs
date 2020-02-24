using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;

namespace Robust.Client.GameObjects
{
    public class ClientEntityNetworkManager : IEntityNetworkManager
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _network;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        /// <inheritdoc />
        public void SetupNetworking()
        {
            _network.RegisterNetMessage<MsgEntity>(MsgEntity.NAME, HandleEntityNetworkMessage);
        }

        /// <summary>
        /// Sends a message to the relevant system(s) server side.
        /// </summary>
        public void SendSystemNetworkMessage(EntitySystemMessage message)
        {
            var msg = _network.CreateNetMessage<MsgEntity>();
            msg.Type = EntityMessageType.SystemMessage;
            msg.SystemMessage = message;
            _network.ClientSendMessage(msg);
        }

        public void SendSystemNetworkMessage(EntitySystemMessage message, INetChannel channel)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void SendComponentNetworkMessage(INetChannel channel, IEntity entity, IComponent component, ComponentMessage message)
        {
            if (!component.NetID.HasValue)
                throw new ArgumentException($"Component {component.Name} does not have a NetID.", nameof(component));

            var msg = _network.CreateNetMessage<MsgEntity>();
            msg.Type = EntityMessageType.ComponentMessage;
            msg.EntityUid = entity.Uid;
            msg.NetId = component.NetID.Value;
            msg.ComponentMessage = message;
            _network.ClientSendMessage(msg);
        }

        /// <inheritdoc />
        public void SendEntityNetworkMessage(IEntity entity, EntityEventArgs message)
        {
            var msg = _network.CreateNetMessage<MsgEntity>();
            msg.Type = EntityMessageType.EntityMessage;
            msg.EntityUid = entity.Uid;
            msg.EntityMessage = message;
            _network.ClientSendMessage(msg);
        }

        /// <summary>
        /// Converts a raw NetIncomingMessage to an IncomingEntityMessage object
        /// </summary>
        /// <param name="message">raw network message</param>
        /// <returns>An IncomingEntityMessage object</returns>
        private void HandleEntityNetworkMessage(MsgEntity message)
        {
            switch (message.Type)
            {
                case EntityMessageType.ComponentMessage:
                    _entityManager.HandleEntityNetworkMessage(message);
                    return;

                case EntityMessageType.EntityMessage:
                    return;

                case EntityMessageType.SystemMessage:
                    _entityManager.EventBus.RaiseEvent(EventSource.Network, message.SystemMessage);
                    return;
            }
        }
    }
}
