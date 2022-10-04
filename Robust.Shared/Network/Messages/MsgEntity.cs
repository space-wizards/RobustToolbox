using System;
using System.IO;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgEntity : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public EntityMessageType Type { get; set; }

        public EntityEventArgs SystemMessage { get; set; }
        public EntityUid EntityUid { get; set; }
        public uint NetId { get; set; }
        public uint Sequence { get; set; }
        public GameTick SourceTick { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            Type = (EntityMessageType)buffer.ReadByte();
            SourceTick = buffer.ReadGameTick();
            Sequence = buffer.ReadUInt32();

            switch (Type)
            {
                case EntityMessageType.SystemMessage:
                {
                    int length = buffer.ReadVariableInt32();
                    using var stream = buffer.ReadAlignedMemory(length);
                    SystemMessage = serializer.Deserialize<EntityEventArgs>(stream);
                }
                    break;
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write((byte)Type);
            buffer.Write(SourceTick);
            buffer.Write(Sequence);

            switch (Type)
            {
                case EntityMessageType.SystemMessage:
                {
                    var stream = new MemoryStream();

                    serializer.Serialize(stream, SystemMessage);

                    buffer.WriteVariableInt32((int)stream.Length);
                    buffer.Write(stream.AsSpan());
                }
                    break;
            }
        }

        public override string ToString()
        {
            var timingData = $"T: {SourceTick} S: {Sequence}";
            switch (Type)
            {
                case EntityMessageType.Error:
                    return "MsgEntity Error";
                case EntityMessageType.SystemMessage:
                    return $"MsgEntity Comp, {timingData}, {SystemMessage}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
