using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    [NetMessage(MsgGroups.String)]
    public class MsgConCmdAck : NetMessage
    {
        public string Text { get; set; }
        public bool Error { get; set; }

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
