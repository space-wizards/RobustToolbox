using System;
using System.Collections.Generic;
using System.IO;
using Lidgren.Network;
using NetSerializer;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

#nullable disable

namespace Robust.Shared.Network.Messages;

public sealed class MsgInput : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Input;

    public NetListAsArray<FullInputCmdMessage> InputMessageList;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        int length = buffer.ReadVariableInt32();
        using var stream = buffer.ReadAlignedMemory(length);
        InputMessageList = serializer.Deserialize<NetListAsArray<FullInputCmdMessage>>(stream);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        var stream = new MemoryStream();
        serializer.Serialize(stream, InputMessageList);
        buffer.WriteVariableInt32((int)stream.Length);
        buffer.Write(stream.AsSpan());
    }
}
