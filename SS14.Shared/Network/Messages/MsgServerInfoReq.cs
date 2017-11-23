using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgServerInfoReq : NetMessage
    {
        #region REQUIRED
        public static readonly string NAME = "ServerInfoReq";
        public static readonly MsgGroups GROUP = MsgGroups.Core;
        public static readonly NetMessages ID = NetMessages.WelcomeMessageReq;

        public MsgServerInfoReq(INetChannel channel)
            : base(NAME, GROUP, ID)
        {}
        #endregion

        public string PlayerName { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            PlayerName = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(PlayerName);
        }
    }
}
