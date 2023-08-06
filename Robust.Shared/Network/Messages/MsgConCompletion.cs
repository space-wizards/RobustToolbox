using Lidgren.Network;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network.Messages;

#nullable disable

public sealed class MsgConCompletion : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public int Seq { get; set; }
    public string[] Args { get; set; }

    public string ArgString { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Seq = buffer.ReadInt32();

        var len = buffer.ReadVariableInt32();
        Args = new string[len];
        for (var i = 0; i < len; i++)
        {
            Args[i] = buffer.ReadString();
        }

        ArgString = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Seq);

        buffer.WriteVariableInt32(Args.Length);
        foreach (var arg in Args)
        {
            buffer.Write(arg);
        }

        buffer.Write(ArgString);
    }
}
