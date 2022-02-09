using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgScriptStop : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

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
