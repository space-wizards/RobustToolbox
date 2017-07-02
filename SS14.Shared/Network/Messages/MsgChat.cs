using Lidgren.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgChat : NetMessage
    {
        #region REQUIRED
        public static readonly string NAME = "ChatMessage";
        public static readonly MsgGroups GROUP = MsgGroups.CORE;
        public static readonly NetMessages ID = NetMessages.ChatMessage;
        public static ProcessMessage _callback;
        public override ProcessMessage Callback
        {
            get => _callback;
            set => _callback = value;
        }

        public MsgChat(NetChannel channel)
            : base(channel, NAME, GROUP, ID)
        {}
        #endregion

        public ChatChannel channel;
        public string text;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            channel = (ChatChannel)buffer.ReadByte();
            text = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)channel);
            buffer.Write(text);
        }
    }
}
