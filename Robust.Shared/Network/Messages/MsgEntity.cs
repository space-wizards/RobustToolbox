using System;
using System.IO;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgEntity : NetMessage
    {
        [ThreadStatic]
        private static Func<Type, bool> _canReceiveNetworkEvent;

        internal static void SetNetworkEventReceiver(IEventBus eventBus)
        {
            _canReceiveNetworkEvent = eventBus.CanReceiveNetworkEvent;
        }

        internal static void ClearNetworkEventReceiver()
        {
            _canReceiveNetworkEvent = null;
        }

        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public EntityMessageType Type { get; set; }

        public EntityEventArgs SystemMessage { get; set; }
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
                    var length = buffer.ReadVariableInt32();
                    using var stream = RobustMemoryManager.GetMemoryStream(length);
                    buffer.ReadAlignedMemory(stream, length);

                    if (!serializer.TryGetSerializedType(stream, out var eventType))
                    {
                        Logger.GetSawmill("net").Error($"{MsgChannel}: dropping {nameof(MsgEntity)} with unknown entity event type.");
                        Type = EntityMessageType.Error;
                        break;
                    }

                    if (eventType == null || !typeof(EntityEventArgs).IsAssignableFrom(eventType))
                    {
                        Logger.GetSawmill("net").Error($"{MsgChannel}: dropping invalid entity event type {eventType?.Name ?? "<null>"}.");
                        Type = EntityMessageType.Error;
                        break;
                    }

                    if (!CanReceiveNetworkEvent(eventType))
                    {
                        Logger.GetSawmill("net").Debug($"{MsgChannel}: dropping unhandled entity event {eventType.Name}.");
                        Type = EntityMessageType.Error;
                        break;
                    }

                    SystemMessage = serializer.Deserialize<EntityEventArgs>(stream);
                    NetSizeStats.Record(NetSizeStatKind.EntityEvent, SystemMessage.GetType(), length);
                    NetSizeStats.RecordSerializableMembers(SystemMessage, serializer);
                    break;
                }
                default:
                    Type = EntityMessageType.Error;
                    Logger.GetSawmill("net").Error($"{MsgChannel}: dropping {nameof(MsgEntity)} with unknown EntityMessageType.");
                    break;
            }
        }

        private static bool CanReceiveNetworkEvent(Type eventType)
        {
            return _canReceiveNetworkEvent?.Invoke(eventType) == true;
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
