using System;
using System.Collections.Generic;
using ClientInterfaces.Network;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13.IoC;
using ClientInterfaces.MessageLogging;
using ClientInterfaces.Configuration;

namespace CGO
{
    public class EntityNetworkManager
    {
        private readonly INetworkManager _networkManager;
        private bool _messageProfiling;

        public EntityNetworkManager(INetworkManager networkManager)
        {
            _networkManager = networkManager;
            _messageProfiling = IoCManager.Resolve<IConfigurationManager>().GetMessageLogging();
        }

        public NetOutgoingMessage CreateEntityMessage()
        {
            NetOutgoingMessage message = _networkManager.CreateMessage();
            message.Write((byte)NetMessage.EntityMessage);
            return message;
        }

        #region Sending
        /// <summary>
        /// Allows a component owned by this entity to send a message to a counterpart component on the
        /// counterpart entities on all clients.
        /// </summary>
        /// <param name="sendingEntity">Entity sending the message (also entity to send to)</param>   
        /// <param name="family">Family of the component sending the message</param>
        /// <param name="method">Net delivery method -- if null, defaults to NetDeliveryMethod.ReliableUnordered</param>
        /// <param name="messageParams">Parameters of the message</param>
        public void SendComponentNetworkMessage(Entity sendingEntity, ComponentFamily family, NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered, params object[] messageParams)
        {
            var message = CreateEntityMessage();
            message.Write(sendingEntity.Uid);//Write this entity's UID
            message.Write((byte)EntityMessage.ComponentMessage);
            message.Write((byte)family);
            PackParams(message, messageParams);

            if (_messageProfiling)
            {//Log the message
                IMessageLogger logger = IoCManager.Resolve<IMessageLogger>();
                logger.LogOutgoingComponentNetMessage(sendingEntity.Uid, family, messageParams);
            }
            
            //Send the message
            _networkManager.SendMessage(message, method);
        }

        private void PackParams(NetOutgoingMessage message, params object[] messageParams)
        {
            foreach (object messageParam in messageParams)
            {
                Type t = messageParam.GetType();
                if (messageParam.GetType().IsSubclassOf(typeof(Enum)))
                {
                    message.Write((byte)NetworkDataType.d_enum);
                    message.Write((int)messageParam);//Cast to int, because enums are stored as ints anyway.
                }
                else if (messageParam.GetType() == typeof(bool))
                {
                    message.Write((byte)NetworkDataType.d_bool);
                    message.Write((bool)messageParam);
                }
                else if (messageParam.GetType() == typeof(byte))
                {
                    message.Write((byte)NetworkDataType.d_byte);
                    message.Write((byte)messageParam);
                }
                else if (messageParam.GetType() == typeof(sbyte))
                {
                    message.Write((byte)NetworkDataType.d_sbyte);
                    message.Write((sbyte)messageParam);
                }
                else if (messageParam.GetType() == typeof(ushort))
                {
                    message.Write((byte)NetworkDataType.d_ushort);
                    message.Write((ushort)messageParam);
                }
                else if (messageParam.GetType() == typeof(short))
                {
                    message.Write((byte)NetworkDataType.d_short);
                    message.Write((short)messageParam);
                }
                else if (messageParam.GetType() == typeof(int))
                {
                    message.Write((byte)NetworkDataType.d_int);
                    message.Write((int)messageParam);
                }
                else if (messageParam.GetType() == typeof(uint))
                {
                    message.Write((byte)NetworkDataType.d_uint);
                    message.Write((uint)messageParam);
                }
                else if (messageParam.GetType() == typeof(ulong))
                {
                    message.Write((byte)NetworkDataType.d_ulong);
                    message.Write((ulong)messageParam);
                }
                else if (messageParam.GetType() == typeof(long))
                {
                    message.Write((byte)NetworkDataType.d_long);
                    message.Write((long)messageParam);
                }
                else if (messageParam.GetType() == typeof(float))
                {
                    message.Write((byte)NetworkDataType.d_float);
                    message.Write((float)messageParam);
                }
                else if (messageParam.GetType() == typeof(double))
                {
                    message.Write((byte)NetworkDataType.d_double);
                    message.Write((double)messageParam);
                }
                else if (messageParam.GetType() == typeof(string))
                {
                    message.Write((byte)NetworkDataType.d_string);
                    message.Write((string)messageParam);
                }
                else
                {
                    throw new NotImplementedException("Cannot write specified type.");
                }
            }
        }

        /// <summary>
        /// Sends an arbitrary entity network message
        /// </summary>
        /// <param name="sendingEntity">The entity the message is going from(and to, on the other end)</param>
        /// <param name="type">Message type</param>
        /// <param name="list">List of parameter objects</param>
        public void SendEntityNetworkMessage(Entity sendingEntity, EntityMessage type, params object[] list)
        {
            NetOutgoingMessage message = CreateEntityMessage();
            message.Write(sendingEntity.Uid);//Write this entity's UID
            message.Write((byte)type);
            PackParams(message, list);
            _networkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        /// <summary>
        /// Sends an SVar to the server to be set on the server-side entity.
        /// </summary>
        /// <param name="sendingEntity"></param>
        /// <param name="svar"></param>
        public void SendSVar(Entity sendingEntity, MarshalComponentParameter svar)
        {
            var message = CreateEntityMessage();
            message.Write(sendingEntity.Uid);
            message.Write((byte)EntityMessage.SetSVar);
            svar.Serialize(message);
            _networkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        #endregion

        #region Receiving
        /// <summary>
        /// Converts a raw NetIncomingMessage to an IncomingEntityMessage object
        /// </summary>
        /// <param name="message">raw network message</param>
        /// <returns>An IncomingEntityMessage object</returns>
        public IncomingEntityMessage HandleEntityNetworkMessage(NetIncomingMessage message)
        {
            var uid = message.ReadInt32();
            var messageType = (EntityMessage)message.ReadByte();
            var result = IncomingEntityMessage.Null;

            switch (messageType)
            {
                case EntityMessage.ComponentMessage:
                    var messageContent = HandleEntityComponentNetworkMessage(message);
                    result = new IncomingEntityMessage(uid, EntityMessage.ComponentMessage, messageContent, message.SenderConnection);

                    if (_messageProfiling)
                    {
                        //Log the message
                        IMessageLogger logger = IoCManager.Resolve<IMessageLogger>();
                        logger.LogIncomingComponentNetMessage(result.Uid, result.MessageType, messageContent.ComponentFamily, messageContent.MessageParameters.ToArray());
                    }

                    break;
                case EntityMessage.PositionMessage:
                    //TODO: Handle position messages!
                    break;
                case EntityMessage.GetSVars:
                    result = new IncomingEntityMessage(uid, EntityMessage.GetSVars, message, message.SenderConnection);
                    break;
                case EntityMessage.SetDirection:
                    result = new IncomingEntityMessage(uid, EntityMessage.SetDirection, message.ReadByte(), message.SenderConnection);
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
            ComponentFamily componentFamily = (ComponentFamily)message.ReadByte();
            List<object> messageParams = new List<object>();
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
                }
            }
            return new IncomingEntityComponentMessage(componentFamily, messageParams);
        }
        #endregion
    }


}
