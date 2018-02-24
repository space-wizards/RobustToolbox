using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;

namespace SS14.Client.GameObjects
{
    public class ClientEntityNetworkManager : IEntityNetworkManager
    {
        [Dependency]
        private readonly IClientNetManager _network;

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

        /// <inheritdoc />
        public void SendDirectedComponentNetworkMessage(INetChannel channel, IEntity entity, IComponent component, ComponentMessage message)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SendComponentNetworkMessage(IEntity entity, IComponent component, ComponentMessage message)
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
        public IncomingEntityMessage HandleEntityNetworkMessage(MsgEntity message)
        {
            switch (message.Type)
            {
                case EntityMessageType.ComponentMessage:
                    return new IncomingEntityMessage(message);

                case EntityMessageType.EntityMessage:
                    // TODO: Handle this.
                    break;

                case EntityMessageType.SystemMessage: //TODO: Not happy with this resolving the entmgr everytime a message comes in.
                    var manager = IoCManager.Resolve<IEntitySystemManager>();
                    manager.HandleSystemMessage(message);
                    break;
            }
            return null;
        }
    }
}
