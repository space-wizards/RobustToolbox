using Lidgren.Network;
using Robust.Shared.Interfaces.Network;

namespace Robust.Shared.Network.Messages
{
    public class MsgPlayerListReq : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Core;
        public static readonly string NAME = nameof(MsgPlayerListReq);
        public MsgPlayerListReq(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
