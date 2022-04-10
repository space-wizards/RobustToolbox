using Lidgren.Network;

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgPlayerListReq : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Core;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
