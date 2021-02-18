using Lidgren.Network;
using Robust.Shared.Timing;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgStateAck : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Entity;
        public static readonly string NAME = nameof(MsgStateAck);
        public MsgStateAck(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

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
