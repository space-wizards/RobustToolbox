using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    [NetMessage(MsgGroups.Core)]
    public class MsgPlayerListReq : NetMessage
    {
        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
