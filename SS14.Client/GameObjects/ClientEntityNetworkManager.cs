using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Sends an arbitrary entity network message
        /// </summary>
        /// <param name="sendingEntity">The entity the message is going from(and to, on the other end)</param>
        /// <param name="type">Message type</param>
        /// <param name="list">List of parameter objects</param>
        public void SendEntityNetworkMessage(IEntity sendingEntity, EntityMessageType type, params object[] list)
        {
            var msg = _network.CreateNetMessage<MsgEntity>();
            msg.Type = type;
            msg.EntityUid = sendingEntity.Uid;
            msg.Parameters = new List<object>(list);
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
                case EntityMessageType.SystemMessage: //TODO: Not happy with this resolving the entmgr everytime a message comes in.
                    var manager = IoCManager.Resolve<IEntitySystemManager>();
                    manager.HandleSystemMessage(message);
                    break;
            }
            return null;
        }
    }
}
