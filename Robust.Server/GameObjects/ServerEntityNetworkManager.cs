using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;

namespace Robust.Server.GameObjects
{
    public class ServerEntityNetworkManager : IEntityNetworkManager
    {
        [Dependency]
#pragma warning disable 649
        private readonly IServerNetManager _mNetManager;
        [Dependency]
        private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        #region IEntityNetworkManager Members

        /// <inheritdoc />
        public void SendEntityNetworkMessage(IEntity entity, EntityEventArgs message)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SendComponentNetworkMessage(INetChannel channel, IEntity entity, IComponent component, ComponentMessage message)
        {
            if (_mNetManager.IsClient)
                return;

            if (!component.NetID.HasValue)
                throw new ArgumentException($"Component {component.Name} does not have a NetID.", nameof(component));

            var msg = _mNetManager.CreateNetMessage<MsgEntity>();
            msg.Type = EntityMessageType.ComponentMessage;
            msg.EntityUid = entity.Uid;
            msg.NetId = component.NetID.Value;
            msg.ComponentMessage = message;

            //Send the message
            if (channel == null)
            {
                _mNetManager.ServerSendToAll(msg);
            }
            else
            {
                _mNetManager.ServerSendMessage(msg, channel);
            }
        }

        #endregion IEntityNetworkManager Members

        #region Sending
        
        /// <summary>
        /// Sends a message to the relevant system(s) on all clients.
        /// </summary>
        public void SendSystemNetworkMessage(EntitySystemMessage message)
        {
            var newMsg = _mNetManager.CreateNetMessage<MsgEntity>();
            newMsg.Type = EntityMessageType.SystemMessage;
            newMsg.SystemMessage = message;

            _mNetManager.ServerSendToAll(newMsg);
        }

        /// <summary>
        /// Sends a message to the relevant system(s) on the target client.
        /// </summary>
        public void SendSystemNetworkMessage(EntitySystemMessage message, INetChannel targetConnection)
        {
            var newMsg = _mNetManager.CreateNetMessage<MsgEntity>();
            newMsg.Type = EntityMessageType.SystemMessage;
            newMsg.SystemMessage = message;

            _mNetManager.ServerSendMessage(newMsg, targetConnection);
        }

        #endregion Sending

        /// <summary>
        /// Handles an incoming entity message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public IncomingEntityMessage HandleEntityNetworkMessage(MsgEntity message)
        {
            switch (message.Type)
            {
                case EntityMessageType.ComponentMessage:
                    return new IncomingEntityMessage(message);

                case EntityMessageType.EntityMessage:
                    return null;

                case EntityMessageType.SystemMessage:
                    _entitySystemManager.HandleSystemMessage(message);
                    break;
            }
            return null;
        }
    }
}
