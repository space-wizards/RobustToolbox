using System;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgConCmdAck : NetMessage
    {
        #region REQUIRED
        public const NetMessages ID = NetMessages.ConsoleCommandReply;
        public const MsgGroups GROUP = MsgGroups.STRING;

        public static readonly string NAME = ID.ToString();
        public MsgConCmdAck(INetChannel channel) : base(NAME, GROUP, ID) { }
        #endregion

        public string Text;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Text = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Text);
        }
    }
}
