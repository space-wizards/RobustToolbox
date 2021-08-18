using System;
using System.Collections.Generic;
using System.IO;
using Lidgren.Network;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgEntity : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

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
