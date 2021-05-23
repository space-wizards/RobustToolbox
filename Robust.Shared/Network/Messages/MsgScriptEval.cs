using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    [NetMessage(MsgGroups.Command)]
    public class MsgScriptEval : NetMessage
    {
        public int ScriptSession { get; set; }
        public string Code { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ScriptSession = buffer.ReadInt32();
            Code = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ScriptSession);
            buffer.Write(Code);
        }
    }
}
