using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgChat : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.ChatMessage;
        public static readonly MsgGroups GROUP = MsgGroups.STRING;

        public static readonly string NAME = ID.ToString();
        public MsgChat(INetChannel channel) : base(NAME, GROUP, ID) { }
        #endregion

        public ChatChannel channel;
        public string text;
        public int? entityId;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            channel = (ChatChannel)buffer.ReadByte();
            text = buffer.ReadString();

            var id = buffer.ReadInt32();
            if (id == -1)
                entityId = null;
            else
                entityId = id;
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)channel);
            buffer.Write(text);
            if (entityId == null)
                buffer.Write(-1);
            else
                buffer.Write((int)entityId);

        }
    }
}
