using Lidgren.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgSession : NetMessage
    {
        #region REQUIRED
        public static readonly string NAME = "PlayerSessionMessage";
        public static readonly MsgGroups GROUP = MsgGroups.CORE;
        public static readonly NetMessages ID = NetMessages.PlayerSessionMessage;
        public static ProcessMessage _callback;
        public override ProcessMessage Callback
        {
            get => _callback;
            set => _callback = value;
        }

        public MsgSession(NetChannel channel)
            : base(channel, NAME, GROUP, ID)
        { }
        #endregion

        public PlayerSessionMessage msgType;
        public string verb;
        public int uid;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            msgType = (PlayerSessionMessage) buffer.ReadByte();
            verb = buffer.ReadString();
            uid = buffer.ReadInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)msgType);
            buffer.Write(verb);
            buffer.Write(uid);
        }
    }
}
