using Lidgren.Network;
using NetSerializer;
using SS14.Client.Interfaces.MessageLogging;
using SS14.Client.Interfaces.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.IO;

namespace SS14.Client.GameObjects
{
    public class ClientEntityNetworkManager : IEntityNetworkManager, IPostInjectInit
    {
        private bool _messageProfiling = false;
        [Dependency]
        private readonly INetworkManager _networkManager;
        [Dependency]
        private readonly IConfigurationManager _configManager;

        public void PostInject()
        {
            _configManager.RegisterCVar("ent.logging", false);
        }

        public void Initialize()
        {
            _messageProfiling = _configManager.GetCVar<bool>("ent.logging");
        }

        #region IEntityNetworkManager Members

        public NetOutgoingMessage CreateEntityMessage()
        {
            NetOutgoingMessage message = _networkManager.CreateMessage();
            message.Write((byte)NetMessage.EntityMessage);
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

            var stream = new MemoryStream();
            Serializer.Serialize(stream, message);
            newMsg.Write((int)stream.Length);
            newMsg.Write(stream.ToArray());

            if (_messageProfiling)
            {
                //Log the message
            }

            //Send the message
            _networkManager.SendMessage(newMsg, method);
        }

        public void SendDirectedComponentNetworkMessage(IEntity sendingEntity, ComponentFamily family,
                                                        NetDeliveryMethod method, NetConnection recipient,
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
        public void SendComponentNetworkMessage(IEntity sendingEntity, ComponentFamily family,
                                                NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered,
                                                params object[] messageParams)
        {
            NetOutgoingMessage message = CreateEntityMessage();
            message.Write((byte)EntityMessage.ComponentMessage);
            message.Write(sendingEntity.Uid); //Write this entity's UID
            message.Write((byte)family);
            PackParams(message, messageParams);

            if (_messageProfiling)
            {
                //Log the message
                var logger = IoCManager.Resolve<IMessageLogger>();
                logger.LogOutgoingComponentNetMessage(sendingEntity.Uid, family, messageParams);
            }

            //Send the message
            _networkManager.SendMessage(message, method);
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
            _networkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
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

        /// <summary>
        /// Sends an SVar to the server to be set on the server-side entity.
        /// </summary>
        /// <param name="sendingEntity"></param>
        /// <param name="svar"></param>
        public void SendSVar(IEntity sendingEntity, MarshalComponentParameter svar)
        {
            NetOutgoingMessage message = CreateEntityMessage();
            message.Write((byte)EntityMessage.SetSVar);
            message.Write(sendingEntity.Uid);
            svar.Serialize(message);
            _networkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        #endregion Sending

        #region Receiving

        /// <summary>
        /// Converts a raw NetIncomingMessage to an IncomingEntityMessage object
        /// </summary>
        /// <param name="message">raw network message</param>
        /// <returns>An IncomingEntityMessage object</returns>
        public IncomingEntityMessage HandleEntityNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (EntityMessage)message.ReadByte();
            int uid;
            IncomingEntityMessage result = IncomingEntityMessage.Null;

            switch (messageType)
            {
                case EntityMessage.ComponentMessage:
                    uid = message.ReadInt32();
                    IncomingEntityComponentMessage messageContent = HandleEntityComponentNetworkMessage(message);
                    result = new IncomingEntityMessage(uid, EntityMessage.ComponentMessage, messageContent,
                                                       message.SenderConnection);

                    if (_messageProfiling)
                    {
                        //Log the message
                        var logger = IoCManager.Resolve<IMessageLogger>();
                        logger.LogIncomingComponentNetMessage(result.Uid, result.MessageType,
                                                              messageContent.ComponentFamily,
                                                              messageContent.MessageParameters.ToArray());
                    }

                    break;
                case EntityMessage.SystemMessage: //TODO: Not happy with this resolving the entmgr everytime a message comes in.
                    var manager = IoCManager.Resolve<IEntitySystemManager>();
                    manager.HandleSystemMessage(new EntitySystemData(message.SenderConnection, message));
                    break;
                case EntityMessage.PositionMessage:
                    uid = message.ReadInt32();
                    //TODO: Handle position messages!
                    break;
                case EntityMessage.GetSVars:
                    uid = message.ReadInt32();
                    result = new IncomingEntityMessage(uid, EntityMessage.GetSVars, message, message.SenderConnection);
                    break;
            }
            return result;
        }

        /// <summary>
        /// Handles an incoming entity component message
        /// </summary>
        /// <param name="message">Raw network message</param>
        /// <returns>An IncomingEntityComponentMessage object</returns>
        public IncomingEntityComponentMessage HandleEntityComponentNetworkMessage(NetIncomingMessage message)
        {
            var componentFamily = (ComponentFamily)message.ReadByte();
            var messageParams = new List<object>();
            while (message.Position < message.LengthBits)
            {
                switch ((NetworkDataType)message.ReadByte())
                {
                    case NetworkDataType.d_enum:
                        messageParams.Add(message.ReadInt32());
                        break;
                    case NetworkDataType.d_bool:
                        messageParams.Add(message.ReadBoolean());
                        break;
                    case NetworkDataType.d_byte:
                        messageParams.Add(message.ReadByte());
                        break;
                    case NetworkDataType.d_sbyte:
                        messageParams.Add(message.ReadSByte());
                        break;
                    case NetworkDataType.d_ushort:
                        messageParams.Add(message.ReadUInt16());
                        break;
                    case NetworkDataType.d_short:
                        messageParams.Add(message.ReadInt16());
                        break;
                    case NetworkDataType.d_int:
                        messageParams.Add(message.ReadInt32());
                        break;
                    case NetworkDataType.d_uint:
                        messageParams.Add(message.ReadUInt32());
                        break;
                    case NetworkDataType.d_ulong:
                        messageParams.Add(message.ReadUInt64());
                        break;
                    case NetworkDataType.d_long:
                        messageParams.Add(message.ReadInt64());
                        break;
                    case NetworkDataType.d_float:
                        messageParams.Add(message.ReadFloat());
                        break;
                    case NetworkDataType.d_double:
                        messageParams.Add(message.ReadDouble());
                        break;
                    case NetworkDataType.d_string:
                        messageParams.Add(message.ReadString());
                        break;
                    case NetworkDataType.d_byteArray:
                        int length = message.ReadInt32();
                        messageParams.Add(message.ReadBytes(length));
                        break;
                }
            }
            return new IncomingEntityComponentMessage(componentFamily, messageParams);
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
