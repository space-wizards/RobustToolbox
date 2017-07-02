using Lidgren.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgPlayerListReq : NetMessage
    {
        #region REQUIRED

        public static readonly string NAME = "PlayerListReq";
        public static readonly MsgGroups GROUP = MsgGroups.CORE;
        public static readonly NetMessages ID = NetMessages.PlayerListReq; //TODO: Remove this and use the StringTable properly.
        public static ProcessMessage _callback;
        public override ProcessMessage Callback
        {
            get => _callback;
            set => _callback = value;
        }

        public MsgPlayerListReq(NetChannel channel)
            : base(channel, NAME, GROUP, ID)
        {
        }

        #endregion

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
