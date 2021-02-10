using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgScriptStop : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgScriptStop);

        public MsgScriptStop(INetChannel channel) : base(NAME, GROUP)
        {
        }

        #endregion

        public int ScriptSession { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ScriptSession = buffer.ReadInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ScriptSession);
        }
    }
}
