using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgUi : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Command;
        public static readonly string NAME = nameof(MsgUi);
        public MsgUi(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public UiManagerMessage UiType { get; set; }
        public GuiComponentType CompType { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            UiType = (UiManagerMessage) buffer.ReadByte();
            CompType = (GuiComponentType) buffer.ReadByte();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte) UiType);
            buffer.Write((byte) CompType);
        }
    }
}
