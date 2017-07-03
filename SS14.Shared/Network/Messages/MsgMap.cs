using Lidgren.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgMap : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.MapMessage;
        public static readonly MsgGroups GROUP = MsgGroups.ENTITY;

        public static readonly string NAME = ID.ToString();
        public static ProcessMessage _callback;
        public override ProcessMessage Callback
        {
            get => _callback;
            set => _callback = value;
        }
        public MsgMap(NetChannel channel)
            : base(channel, NAME, GROUP, ID)
        { }
        #endregion

        public MapMessage MessageType;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            MessageType = (MapMessage) buffer.ReadByte();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)MessageType);
        }
    }
}
