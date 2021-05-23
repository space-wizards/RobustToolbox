using Lidgren.Network;
using Robust.Shared.Timing;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    [NetMessage(MsgGroups.Entity)]
    public class MsgStateAck : NetMessage
    {
        public GameTick Sequence { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Sequence = new GameTick(buffer.ReadUInt32());
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Sequence.Value);
        }
    }
}
