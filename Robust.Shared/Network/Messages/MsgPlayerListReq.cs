using Lidgren.Network;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgPlayerListReq : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Core;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
        }
    }
}
