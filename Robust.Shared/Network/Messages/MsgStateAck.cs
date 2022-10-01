using Lidgren.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgStateAck : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Entity;

        public GameTick Sequence { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            Sequence = new GameTick(buffer.ReadUInt32());
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(Sequence.Value);
        }
    }
}
