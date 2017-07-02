using Lidgren.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgServerInfoReq : NetMessage
    {
        #region REQUIRED
        public static readonly string NAME = "ServerInfoReq";
        public static readonly MsgGroups GROUP = MsgGroups.CORE;
        public static readonly NetMessages ID = NetMessages.WelcomeMessageReq;
        public static ProcessMessage _callback;
        public override ProcessMessage Callback
        {
            get => _callback;
            set => _callback = value;
        }

        public MsgServerInfoReq(NetChannel channel)
            : base(channel, NAME, GROUP, ID)
        {}
        #endregion

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
