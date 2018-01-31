using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;
using System;
using System.Collections.Generic;

namespace SS14.Server.GameObjects
{
    public class ServerEntityNetworkManager : IEntityNetworkManager
    {
        [Dependency]
        private readonly IServerNetManager _mNetManager;

        #region IEntityNetworkManager Members

        /// <summary>
        /// Sends an arbitrary entity network message
        /// </summary>
        /// <param name="sendingEntity">The entity the message is going from(and to, on the other end)</param>
        /// <param name="type">Message type</param>
        /// <param name="list">List of parameter objects</param>
        public void SendEntityNetworkMessage(IEntity sendingEntity, EntityMessageType type, params object[] list)
        {
            throw new NotImplementedException();
        }

        public void SendComponentNetworkMessage(IEntity sendingEntity, uint netId, params object[] messageParams)
        {
            throw new NotImplementedException();
        }

        #endregion IEntityNetworkManager Members

        #region Sending

        /// <summary>
        /// Allows a component owned by this entity to send a message to a counterpart component on the
        /// counterpart entities on all clients.
        /// </summary>
        /// <param name="sendingEntity">Entity sending the message (also entity to send to)</param>
        /// <param name="netId"></param>
        /// <param name="recipient">Client connection to send to. If null, send to all.</param>
        /// <param name="messageParams">Parameters of the message</param>
        public void SendDirectedComponentNetworkMessage(IEntity sendingEntity, uint netId, INetChannel recipient, params object[] messageParams)
        {
            MsgEntity message = _mNetManager.CreateNetMessage<MsgEntity>();
            message.Type = EntityMessageType.ComponentMessage;
            message.EntityUid = sendingEntity.Uid;
            message.NetId = netId;
            message.Parameters = new List<object>(messageParams);

            //Send the message
            if (recipient == null)
            {
                _mNetManager.ServerSendToAll(message);
            }
            else
            {
                _mNetManager.ServerSendMessage(message, recipient);
            }
        }

        /// <summary>
        /// Sends a message to the relevant system(s) on all clients.
        /// </summary>
        public void SendSystemNetworkMessage(EntitySystemMessage message)
        {
            SendSystemNetworkMessage(message, null);
        }

        /// <summary>
        /// Sends a message to the relevant system(s) on the target client.
        /// </summary>
        public void SendSystemNetworkMessage(EntitySystemMessage message, INetChannel targetConnection = null)
        {
            MsgEntity newMsg = _mNetManager.CreateNetMessage<MsgEntity>();
            newMsg.Type = EntityMessageType.SystemMessage;
            newMsg.SystemMessage = message;

            //Send the message
            if (targetConnection != null)
            {
                _mNetManager.ServerSendMessage(newMsg, targetConnection);
            }
            else
            {
                _mNetManager.ServerSendToAll(newMsg);
            }
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
                case EntityMessageType.SystemMessage: //TODO: Not happy with this resolving the entmgr everytime a message comes in.
                    var manager = IoCManager.Resolve<IEntitySystemManager>();
                    manager.HandleSystemMessage(message);
                    break;
            }
            return null;
        }
    }
}
