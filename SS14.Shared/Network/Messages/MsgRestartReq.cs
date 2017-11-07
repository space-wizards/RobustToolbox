using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgRestartReq : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.ForceRestart;
        public static readonly MsgGroups GROUP = MsgGroups.Command;

        public static readonly string NAME = ID.ToString();
        public MsgRestartReq(INetChannel channel) : base(NAME, GROUP, ID) { }
        #endregion


        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
