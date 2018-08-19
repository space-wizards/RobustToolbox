using Lidgren.Network;
using SS14.Shared.Console;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Players;

namespace SS14.Shared.Network.Messages
{
    public class MsgChat : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.String;
        public static readonly string NAME = nameof(MsgChat);
        public MsgChat(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public ChatChannel Channel { get; set; }
        public string Text { get; set; }
        public NetSessionId? SessionId { get; set; }
        public EntityUid? EntityId { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Channel = (ChatChannel)buffer.ReadByte();
            Text = buffer.ReadString();

            var index = buffer.ReadString();
            if (index == "")
                SessionId = null;
            else
                SessionId = new NetSessionId(index);

            var id = buffer.ReadInt32();
            if (id == -1)
                EntityId = null;
            else
                EntityId = new EntityUid(id);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)Channel);
            buffer.Write(Text);

            if (!SessionId.HasValue)
                buffer.Write("");
            else
                buffer.Write(SessionId.Value.Username);

            if (EntityId == null)
                buffer.Write(-1);
            else
                buffer.Write((int)EntityId);

        }
    }
}
