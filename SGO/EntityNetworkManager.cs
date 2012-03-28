using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using ServerServices;
using ServerInterfaces;
using ServerInterfaces.MessageLogging;

namespace SGO
{
    public class EntityNetworkManager
    {
        private NetServer m_netServer;
        private bool _messageProfiling;

        public EntityNetworkManager(NetServer netServer)
        {
            m_netServer = netServer;
            _messageProfiling = ServiceManager.Singleton.Resolve<IConfigManager>().MessageLogging;
        }

        public NetOutgoingMessage CreateEntityMessage()
        {
            NetOutgoingMessage message = m_netServer.CreateMessage();
            message.Write((byte)NetMessage.EntityMessage);
            return message;
        }

        public void SendToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            m_netServer.SendToAll(message, method);
        }

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
        public void SendComponentNetworkMessage(Entity sendingEntity, ComponentFamily family, NetDeliveryMethod method,
            NetConnection recipient, params object[] messageParams)
        {
            NetOutgoingMessage message = CreateEntityMessage();
            message.Write(sendingEntity.Uid);//Write this entity's UID
            message.Write((byte)EntityMessage.ComponentMessage);
            message.Write((byte)family);
            //Loop through the params and write them as is proper
            foreach (object messageParam in messageParams)
            {
                if (messageParam.GetType().IsSubclassOf(typeof(Enum)))
                {
                    message.Write((byte)NetworkDataType.d_enum);
                    message.Write((int)messageParam);
                }
                else if (messageParam is bool)
                {
                    message.Write((byte)NetworkDataType.d_bool);
                    message.Write((bool)messageParam);
                }
                else if (messageParam is byte)
                {
                    message.Write((byte)NetworkDataType.d_byte);
                    message.Write((byte)messageParam);
                }
                else if (messageParam is sbyte)
                {
                    message.Write((byte)NetworkDataType.d_sbyte);
                    message.Write((sbyte)messageParam);
                }
                else if (messageParam is ushort)
                {
                    message.Write((byte)NetworkDataType.d_ushort);
                    message.Write((ushort)messageParam);
                }
                else if (messageParam is short)
                {
                    message.Write((byte)NetworkDataType.d_short);
                    message.Write((short)messageParam);
                }
                else if (messageParam is int)
                {
                    message.Write((byte)NetworkDataType.d_int);
                    message.Write((int)messageParam);
                }
                else if (messageParam is uint)
                {
                    message.Write((byte)NetworkDataType.d_uint);
                    message.Write((uint)messageParam);
                }
                else if (messageParam is ulong)
                {
                    message.Write((byte)NetworkDataType.d_ulong);
                    message.Write((ulong)messageParam);
                }
                else if (messageParam is long)
                {
                    message.Write((byte)NetworkDataType.d_long);
                    message.Write((long)messageParam);
                }
                else if (messageParam is float)
                {
                    message.Write((byte)NetworkDataType.d_float);
                    message.Write((float)messageParam);
                }
                else if (messageParam is double)
                {
                    message.Write((byte)NetworkDataType.d_double);
                    message.Write((double)messageParam);
                }
                else if (messageParam is string)
                {
                    message.Write((byte)NetworkDataType.d_string);
                    message.Write((string)messageParam);
                }
                else
                {
                    throw new NotImplementedException("Cannot write specified type.");
                }
            }
            if(method == null)
                method = NetDeliveryMethod.ReliableUnordered;

            //Send the message
            if (recipient == null)
                m_netServer.SendToAll(message, method);
            else
                m_netServer.SendMessage(message, recipient, method);

            if(_messageProfiling)
            {
                var logger = ServiceManager.Singleton.Resolve<IMessageLogger>();
                logger.LogOutgoingComponentNetMessage(
                    (recipient==null)?0:recipient.RemoteUniqueIdentifier,
                    sendingEntity.Uid,
                    family,
                    PackParamsForLog(messageParams));
                    
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

        #endregion

        #region Receiving
        /// <summary>
        /// Handles an incoming entity message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public IncomingEntityMessage HandleEntityNetworkMessage(NetIncomingMessage message)
        {
            var uid = message.ReadInt32();
            var messageType = (EntityMessage)message.ReadByte();
            IncomingEntityMessage incomingEntityMessage = IncomingEntityMessage.Null;
            switch (messageType)
            {
                case EntityMessage.ComponentMessage:
                    incomingEntityMessage = new IncomingEntityMessage(uid, EntityMessage.ComponentMessage, HandleEntityComponentNetworkMessage(message), message.SenderConnection);
                    break;
                case EntityMessage.PositionMessage:
                    //TODO: Handle position messages!
                    break;
                case EntityMessage.ComponentInstantiationMessage:
                    incomingEntityMessage = new IncomingEntityMessage(uid, EntityMessage.ComponentInstantiationMessage, (ComponentFamily)UnPackParams(message).First(), message.SenderConnection);
                    break;
            }

            if (_messageProfiling)
            {
                var logger = ServiceManager.Singleton.Resolve<IMessageLogger>();

                if(messageType == EntityMessage.ComponentMessage)
                {
                    var messageContent = (IncomingEntityComponentMessage) incomingEntityMessage.message;
                    logger.LogIncomingComponentNetMessage(message.SenderConnection.RemoteUniqueIdentifier,
                                                          uid,
                                                          messageType, 
                                                          messageContent.componentFamily,
                                                          PackParamsForLog(messageContent.messageParameters.ToArray()));
                }
                else if(messageType == EntityMessage.ComponentInstantiationMessage)
                {
                    logger.LogIncomingComponentNetMessage(message.SenderConnection.RemoteUniqueIdentifier,
                        uid,
                        messageType,
                        (ComponentFamily)incomingEntityMessage.message,
                        new object[0]);
                }
            }

            return incomingEntityMessage;
        }

        /// <summary>
        /// Handles an incoming entity component message
        /// </summary>
        /// <param name="message"></param>
        public IncomingEntityComponentMessage HandleEntityComponentNetworkMessage(NetIncomingMessage message)
        {
            ComponentFamily componentFamily = (ComponentFamily)message.ReadByte();
            
            return new IncomingEntityComponentMessage(componentFamily, UnPackParams(message));
        }
        #endregion

        private List<object> UnPackParams(NetIncomingMessage message)
        {
            List<object> messageParams = new List<object>();
            while (message.Position < message.LengthBits)
            {
                switch ((NetworkDataType)message.ReadByte())
                {
                    case NetworkDataType.d_enum:
                        messageParams.Add(message.ReadInt32()); //Cast from int, because enums are ints.
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
            return messageParams;
        }
    }


    public struct IncomingEntityComponentMessage
    {
        public ComponentFamily componentFamily;
        public List<object> messageParameters;

        public IncomingEntityComponentMessage(ComponentFamily _componentFamily, List<object> _messageParameters)
        {
            componentFamily = _componentFamily;
            messageParameters = _messageParameters;
        }
    }

    public struct IncomingEntityMessage
    {
        public int uid;
        public EntityMessage messageType;
        public object message;
        public NetConnection client;

        public IncomingEntityMessage(int _uid, EntityMessage _messageType, object _message, NetConnection _client)
        {
            uid = _uid;
            messageType = _messageType;
            message = _message;
            client = _client;
        }

        public static IncomingEntityMessage Null = new IncomingEntityMessage(0, EntityMessage.Null, null, null);

    }
}
