using System;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using System.IO;
using Robust.Shared.IoC;
using System.Collections.Generic;
using Robust.Shared.Enums;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgEntity : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.EntityEvent;
        public static readonly string NAME = nameof(MsgEntity);
        public MsgEntity(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public EntityMessageType Type { get; set; }

        public EntityEventArgs SystemMessage { get; set; }
#pragma warning disable 618
        public ComponentMessage ComponentMessage { get; set; }
#pragma warning restore 618

        public EntityUid EntityUid { get; set; }
        public uint NetId { get; set; }
        public uint Sequence { get; set; }
        public GameTick SourceTick { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Type = (EntityMessageType)buffer.ReadByte();
            SourceTick = buffer.ReadGameTick();
            Sequence = buffer.ReadUInt32();

            switch (Type)
            {
                case EntityMessageType.SystemMessage:
                {
                    var serializer = IoCManager.Resolve<IRobustSerializer>();
                    int length = buffer.ReadVariableInt32();
                    using var stream = buffer.ReadAlignedMemory(length);
                    SystemMessage = serializer.Deserialize<EntityEventArgs>(stream);
                }
                    break;

                case EntityMessageType.ComponentMessage:
                {
                    EntityUid = new EntityUid(buffer.ReadInt32());
                    NetId = buffer.ReadUInt32();

                    var serializer = IoCManager.Resolve<IRobustSerializer>();
                    int length = buffer.ReadVariableInt32();
                    using var stream = buffer.ReadAlignedMemory(length);
#pragma warning disable 618
                    ComponentMessage = serializer.Deserialize<ComponentMessage>(stream);
#pragma warning restore 618
                }
                    break;
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)Type);
            buffer.Write(SourceTick);
            buffer.Write(Sequence);

            switch (Type)
            {
                case EntityMessageType.SystemMessage:
                {
                    var serializer = IoCManager.Resolve<IRobustSerializer>();
                    using (var stream = new MemoryStream())
                    {
                        serializer.Serialize(stream, SystemMessage);
                        buffer.WriteVariableInt32((int)stream.Length);
                        stream.TryGetBuffer(out var segment);
                        buffer.Write(segment);
                    }
                }
                    break;

                case EntityMessageType.ComponentMessage:
                {
                    buffer.Write((int)EntityUid);
                    buffer.Write(NetId);

                    var serializer = IoCManager.Resolve<IRobustSerializer>();
                    using (var stream = new MemoryStream())
                    {
                        serializer.Serialize(stream, ComponentMessage);
                        buffer.WriteVariableInt32((int)stream.Length);
                        stream.TryGetBuffer(out var segment);
                        buffer.Write(segment);
                    }
                }
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

#if false
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
                        var buf = new byte[length];
                        message.ReadBytes(buf);
                        messageParams.Add(buf)
                        break;
                }
            }
            return messageParams;
        }
#endif

        #endregion Parameter Packing

        public override string ToString()
        {
            var timingData = $"T: {SourceTick} S: {Sequence}";
            switch (Type)
            {
                case EntityMessageType.Error:
                    return "MsgEntity Error";
                case EntityMessageType.ComponentMessage:
                    return $"MsgEntity Comp, {timingData}, {EntityUid}/{NetId}: {ComponentMessage}";
                case EntityMessageType.SystemMessage:
                    return $"MsgEntity Comp, {timingData}, {SystemMessage}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
