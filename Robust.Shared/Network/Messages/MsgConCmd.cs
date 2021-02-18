using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgConCmd : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Command;
        public static readonly string NAME = nameof(MsgConCmd);
        public MsgConCmd(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public string Text { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Text = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Text);
        }
    }
}
