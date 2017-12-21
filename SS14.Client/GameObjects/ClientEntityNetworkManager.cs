using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;
using System;
using System.IO;

namespace SS14.Client.GameObjects
{
    public class ClientEntityNetworkManager : IEntityNetworkManager
    {
        [Dependency]
        private readonly ISS14Serializer serializer;
        [Dependency]
        private readonly IClientNetManager _networkManager;

        #region IEntityNetworkManager Members

        public NetOutgoingMessage CreateEntityMessage()
        {
            NetOutgoingMessage message = _networkManager.CreateMessage();
            message.Write((byte)NetMessages.EntityMessage);
            return message;
        }

        #endregion IEntityNetworkManager Members

        #region Sending

        /// <summary>
        /// Sends a message to the relevant system(s) serverside.
        /// </summary>
        public void SendSystemNetworkMessage(EntitySystemMessage message,
                                             NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered)
        {
            NetOutgoingMessage newMsg = CreateEntityMessage();
            newMsg.Write((byte)EntityMessage.SystemMessage);

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, message);
                newMsg.Write((int)stream.Length);
                newMsg.Write(stream.ToArray());
            }

            //Send the message
            _networkManager.ClientSendMessage(newMsg, method);
        }

        public void SendDirectedComponentNetworkMessage(IEntity sendingEntity, uint netID,
                                                        NetDeliveryMethod method, INetChannel recipient,
                                                        params object[] messageParams)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Allows a component owned by this entity to send a message to a counterpart component on the
        /// counterpart entities on all clients.
        /// </summary>
        /// <param name="sendingEntity">Entity sending the message (also entity to send to)</param>
        /// <param name="family">Family of the component sending the message</param>
        /// <param name="method">Net delivery method -- if null, defaults to NetDeliveryMethod.ReliableUnordered</param>
        /// <param name="messageParams">Parameters of the message</param>
        public void SendComponentNetworkMessage(IEntity sendingEntity, uint netID,
                                                NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered,
                                                params object[] messageParams)
        {
            NetOutgoingMessage message = CreateEntityMessage();
            message.Write((byte)EntityMessage.ComponentMessage);
            message.Write(sendingEntity.Uid); //Write this entity's UID
            message.Write(netID);
            PackParams(message, messageParams);

            //Send the message
            _networkManager.ClientSendMessage(message, method);
        }

        /// <summary>
        /// Sends an arbitrary entity network message
        /// </summary>
        /// <param name="sendingEntity">The entity the message is going from(and to, on the other end)</param>
        /// <param name="type">Message type</param>
        /// <param name="list">List of parameter objects</param>
        public void SendEntityNetworkMessage(IEntity sendingEntity, EntityMessage type, params object[] list)
        {
            NetOutgoingMessage message = CreateEntityMessage();
            message.Write((byte)type);
            message.Write(sendingEntity.Uid); //Write this entity's UID
            PackParams(message, list);
            _networkManager.ClientSendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        private void PackParams(NetOutgoingMessage message, params object[] messageParams)
        {
            foreach (object messageParam in messageParams)
            {
                switch (messageParam)
                {
                    case Enum val:
                        message.Write((byte)NetworkDataType.d_enum);
                        message.Write(Convert.ToInt32(val));
                        break;

                    case bool val:
                        message.Write((byte)NetworkDataType.d_bool);
                        message.Write(val);
                        break;

                    case byte val:
                        message.Write((byte)NetworkDataType.d_byte);
                        message.Write(val);
                        break;

                    case sbyte val:
                        message.Write((byte)NetworkDataType.d_sbyte);
                        message.Write(val);
                        break;

                    case ushort val:
                        message.Write((byte)NetworkDataType.d_ushort);
                        message.Write(val);
                        break;

                    case short val:
                        message.Write((byte)NetworkDataType.d_short);
                        message.Write(val);
                        break;

                    case int val:
                        message.Write((byte)NetworkDataType.d_int);
                        message.Write(val);
                        break;

                    case uint val:
                        message.Write((byte)NetworkDataType.d_uint);
                        message.Write(val);
                        break;

                    case ulong val:
                        message.Write((byte)NetworkDataType.d_ulong);
                        message.Write(val);
                        break;

                    case long val:
                        message.Write((byte)NetworkDataType.d_long);
                        message.Write(val);
                        break;

                    case float val:
                        message.Write((byte)NetworkDataType.d_float);
                        message.Write(val);
                        break;

                    case double val:
                        message.Write((byte)NetworkDataType.d_double);
                        message.Write(val);
                        break;

                    case string val:
                        message.Write((byte)NetworkDataType.d_string);
                        message.Write(val);
                        break;

                    case Byte[] val:
                        message.Write((byte)NetworkDataType.d_byteArray);
                        message.Write(val.Length);
                        message.Write(val);
                        break;

                    default:
                        throw new NotImplementedException("Cannot write specified type.");
                }
            }
        }

        #endregion Sending

        #region Receiving

        /// <summary>
        /// Converts a raw NetIncomingMessage to an IncomingEntityMessage object
        /// </summary>
        /// <param name="message">raw network message</param>
        /// <returns>An IncomingEntityMessage object</returns>
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

        #region dummy methods

        public void SendMessage(NetOutgoingMessage message, NetConnection recipient,
                                NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            throw new NotImplementedException();
        }

        #endregion dummy methods
    }
}
