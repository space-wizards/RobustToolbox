using Lidgren.Network;
using SS14.Shared.Console;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgChat : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.ChatMessage;
        public static readonly MsgGroups GROUP = MsgGroups.String;

        public static readonly string NAME = ID.ToString();
        public MsgChat(INetChannel channel) : base(NAME, GROUP, ID) { }
        #endregion

        public ChatChannel Channel { get; set; }
        public string Text { get; set; }
        public int? EntityId { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Channel = (ChatChannel)buffer.ReadByte();
            Text = buffer.ReadString();

            var id = buffer.ReadInt32();
            if (id == -1)
                EntityId = null;
            else
                EntityId = id;
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)Channel);
            buffer.Write(Text);
            if (EntityId == null)
                buffer.Write(-1);
            else
                buffer.Write((int)EntityId);

        }
    }
}
