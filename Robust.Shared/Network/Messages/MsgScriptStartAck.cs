using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgScriptStartAck : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgScriptStartAck);

        public MsgScriptStartAck(INetChannel channel) : base(NAME, GROUP)
        {
        }

        #endregion

        public bool WasAccepted { get; set; }
        public int ScriptSession { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            WasAccepted = buffer.ReadBoolean();
            ScriptSession = buffer.ReadInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(WasAccepted);
            buffer.Write(ScriptSession);
        }
    }
}
