using Lidgren.Network;

namespace Robust.Shared.Network.Messages;

#nullable disable

public sealed class MsgConCompletion : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public int Seq { get; set; }
    public string[] Args { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        Seq = buffer.ReadInt32();

        var len = buffer.ReadVariableInt32();
        Args = new string[len];
        for (var i = 0; i < len; i++)
        {
            Args[i] = buffer.ReadString();
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        buffer.Write(Seq);

        buffer.WriteVariableInt32(Args.Length);
        foreach (var arg in Args)
        {
            buffer.Write(arg);
        }
    }
}
