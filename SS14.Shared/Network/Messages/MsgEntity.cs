using System;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.GameObjects;
using System.IO;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using System.Collections.Generic;

namespace SS14.Shared.Network.Messages
{
    public class MsgEntity : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.EntityMessage;
        public static readonly MsgGroups GROUP = MsgGroups.Entity;

        public static readonly string NAME = ID.ToString();
        public MsgEntity(INetChannel channel)
            : base(NAME, GROUP, ID)
        { }
        #endregion

        public EntityMessage Type { get; set; }

        public EntitySystemMessage SystemMessage { get; set; }
        public NetConnection Sender { get; set; }

        public int EntityId { get; set; }
        public uint NetId { get; set; }
        public List<object> Parameters { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Type = (EntityMessage)buffer.ReadByte();
            Sender = buffer.SenderConnection;

            switch (Type)
            {
                case EntityMessage.SystemMessage:
                    ISS14Serializer serializer = IoCManager.Resolve<ISS14Serializer>();
                    int messageLength = buffer.ReadInt32();
                    using (var stream = new MemoryStream(buffer.ReadBytes(messageLength)))
                    {
                        SystemMessage = serializer.Deserialize<EntitySystemMessage>(stream);
                    }
                    break;
                case EntityMessage.ComponentMessage:
                    EntityId = buffer.ReadInt32();
                    NetId = buffer.ReadUInt32();
                    Parameters = UnPackParams(buffer);
                    break;
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)Type);

            switch (Type)
            {
                case EntityMessage.SystemMessage:
                    ISS14Serializer serializer = IoCManager.Resolve<ISS14Serializer>();
                    using (var stream = new MemoryStream())
                    {
                        serializer.Serialize(stream, SystemMessage);
                        buffer.Write((int)stream.Length);
                        buffer.Write(stream.ToArray());
                    }
                    break;
                case EntityMessage.ComponentMessage:
                    buffer.Write(EntityId);
                    buffer.Write(NetId);
                    PackParams(buffer, Parameters);
                    break;
            }
        }

        #region Parameter Packing

        private void PackParams(NetOutgoingMessage message, List<object> messageParams)
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

        private List<object> UnPackParams(NetIncomingMessage message)
        {
            var messageParams = new List<object>();
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
                    case NetworkDataType.d_byteArray:
                        int length = message.ReadInt32();
                        messageParams.Add(message.ReadBytes(length));
                        break;
                }
            }
            return messageParams;
        }

        #endregion Parameter Packing
    }
}
