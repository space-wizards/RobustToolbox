using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SS14.Server.GameObjects
{
    public class ServerEntityNetworkManager : IEntityNetworkManager
    {
        [Dependency]
        private readonly IServerNetManager _mNetManager;

        #region IEntityNetworkManager Members

        public void SendToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            _mNetManager.ServerSendToAll(message, method);
        }

        public void SendMessage(NetOutgoingMessage message, NetConnection recipient,
                                NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            _mNetManager.ServerSendMessage(message, recipient, method);
        }

        /// <summary>
        /// Sends an arbitrary entity network message
        /// </summary>
        /// <param name="sendingEntity">The entity the message is going from(and to, on the other end)</param>
        /// <param name="type">Message type</param>
        /// <param name="list">List of parameter objects</param>
        public void SendEntityNetworkMessage(IEntity sendingEntity, EntityMessage type, params object[] list)
        {
            throw new NotImplementedException();
        }

        public void SendComponentNetworkMessage(IEntity sendingEntity, uint netID,
                                                NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered, params object[] messageParams)
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
        /// <param name="family">Family of the component sending the message</param>
        /// <param name="method">Net delivery method -- if null, defaults to NetDeliveryMethod.ReliableUnordered</param>
        /// <param name="recipient">Client connection to send to. If null, send to all.</param>
        /// <param name="messageParams">Parameters of the message</param>
        public void SendDirectedComponentNetworkMessage(IEntity sendingEntity, uint netID,
                                                        NetDeliveryMethod method,
                                                        INetChannel recipient, params object[] messageParams)
        {
            MsgEntity message = _mNetManager.CreateNetMessage<MsgEntity>();
            message.Type = EntityMessage.ComponentMessage;
            message.EntityId = sendingEntity.Uid;
            message.NetId = netID;
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

        private object[] PackParamsForLog(params object[] messageParams)
        {
            var parameters = new List<object>();
            foreach (object messageParam in messageParams)
            {
                if (messageParam.GetType().IsSubclassOf(typeof(Enum)))
                {
                    parameters.Add((int)messageParam);
                }
                else if (messageParam is int
                         || messageParam is uint
                         || messageParam is short
                         || messageParam is ushort
                         || messageParam is long
                         || messageParam is ulong
                         || messageParam is bool
                         || messageParam is float
                         || messageParam is double
                         || messageParam is byte
                         || messageParam is sbyte
                         || messageParam is string)
                {
                    parameters.Add(messageParam);
                }
            }

            return parameters.ToArray();
        }

        /// <summary>
        /// Sends a message to the relevant system(s) on all clients.
        /// </summary>
        public void SendSystemNetworkMessage(EntitySystemMessage message,
                                             NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered)
        {
            SendSystemNetworkMessage(message, null, method);
        }

        /// <summary>
        /// Sends a message to the relevant system(s) on the target client.
        /// </summary>
        public void SendSystemNetworkMessage(EntitySystemMessage message,
                                     INetChannel targetConnection = null,
                                     NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered)
        {
            MsgEntity newMsg = _mNetManager.CreateNetMessage<MsgEntity>();
            newMsg.Type = EntityMessage.SystemMessage;
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

        #region Receiving

        /// <summary>
        /// Handles an incoming entity message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public IncomingEntityMessage HandleEntityNetworkMessage(MsgEntity message)
        {
            switch (message.Type)
            {
                case EntityMessage.ComponentMessage:
                    return new IncomingEntityMessage(message);
                case EntityMessage.SystemMessage: //TODO: Not happy with this resolving the entmgr everytime a message comes in.
                    var manager = IoCManager.Resolve<IEntitySystemManager>();
                    manager.HandleSystemMessage(message);
                    break;
                case EntityMessage.PositionMessage:
                    //TODO: Handle position messages!
                    break;
            }
            return null;
        }

        #endregion Receiving
    }
}
