using Lidgren.Network;
using Robust.Shared.Interfaces.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgSetTickRate : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Core;
        public static readonly string NAME = nameof(MsgSetTickRate);
        public MsgSetTickRate(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public byte NewTickRate { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            NewTickRate = buffer.ReadByte();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(NewTickRate);
        }
    }
}
