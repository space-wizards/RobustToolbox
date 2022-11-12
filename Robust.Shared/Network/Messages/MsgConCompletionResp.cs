using Lidgren.Network;
using Robust.Shared.Console;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network.Messages;

#nullable disable

public sealed class MsgConCompletionResp : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public int Seq { get; set; }
    public CompletionResult Result { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Seq = buffer.ReadInt32();

        var len = buffer.ReadVariableInt32();
        var options = new CompletionOption[len];
        for (var i = 0; i < len; i++)
        {
            var optValue = buffer.ReadString();
            var optHint = buffer.ReadString();
            var optFlags = buffer.ReadInt32();

            options[i] = new CompletionOption(
                optValue,
                optHint == "" ? null : optHint,
                (CompletionOptionFlags) optFlags);
        }

        var hint = buffer.ReadString();

        Result = new CompletionResult(options, hint == "" ? null : hint);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Seq);

        buffer.WriteVariableInt32(Result.Options.Length);
        foreach (var option in Result.Options)
        {
            buffer.Write(option.Value);
            buffer.Write(option.Hint);
            buffer.Write((int) option.Flags);
        }

        buffer.Write(Result.Hint);
    }
}
