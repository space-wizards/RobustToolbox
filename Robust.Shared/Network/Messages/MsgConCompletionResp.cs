using Lidgren.Network;
using Robust.Shared.Console;

namespace Robust.Shared.Network.Messages;

#nullable disable

public sealed class MsgConCompletionResp : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public int Seq { get; set; }
    public CompletionResult Result { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        Seq = buffer.ReadInt32();

        var len = buffer.ReadVariableInt32();
        var options = new string[len];
        for (var i = 0; i < len; i++)
        {
            options[i] = buffer.ReadString();
        }

        var hint = buffer.ReadString();

        Result = new CompletionResult(options, hint);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        buffer.Write(Seq);

        buffer.WriteVariableInt32(Result.Options.Length);
        foreach (var option in Result.Options)
        {
            buffer.Write(option);
        }

        buffer.Write(Result.Hint);
    }
}
