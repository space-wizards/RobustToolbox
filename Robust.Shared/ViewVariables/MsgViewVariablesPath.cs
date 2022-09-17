using System;
using System.IO;
using Lidgren.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.ViewVariables;

internal abstract class MsgViewVariablesPath : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public uint RequestId { get; set; } = 0;
    public string Path { get; set; } = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        RequestId = buffer.ReadUInt32();
        Path = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer , IRobustSerializer serializer)
    {
        buffer.Write(RequestId);
        buffer.Write(Path);
    }
}

internal abstract class MsgViewVariablesPathReq : MsgViewVariablesPath
{
    public Guid Session { get; set; } = Guid.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        base.ReadFromBuffer(buffer, serializer);
        Session = buffer.ReadGuid();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        base.WriteToBuffer(buffer, serializer);
        buffer.Write(Session);
    }
}

internal abstract class MsgViewVariablesPathReqVal : MsgViewVariablesPathReq
{
    public string? Value { get; set; } = null;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        base.ReadFromBuffer(buffer, serializer);
        Value = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        base.WriteToBuffer(buffer, serializer);
        buffer.Write(Value);
    }
}

internal abstract class MsgViewVariablesPathRes : MsgViewVariablesPath
{
    public string[] Response { get; set; } = Array.Empty<string>();
    public ViewVariablesResponseCode ResponseCode { get; set; } = ViewVariablesResponseCode.Ok;

    internal MsgViewVariablesPathRes()
    {
    }

    internal MsgViewVariablesPathRes(MsgViewVariablesPathReq req)
    {
        Path = req.Path;
        RequestId = req.RequestId;
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        base.ReadFromBuffer(buffer, serializer);
        ResponseCode = (ViewVariablesResponseCode) buffer.ReadUInt16();
        var length = buffer.ReadInt32();
        Response = new string[length];

        for (var i = 0; i < length; i++)
        {
            Response[i] = buffer.ReadString();
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        base.WriteToBuffer(buffer, serializer);
        buffer.Write((ushort)ResponseCode);
        buffer.Write(Response.Length);

        foreach (var value in Response)
        {
            buffer.Write(value);
        }
    }
}

internal sealed class MsgViewVariablesReadPathReq : MsgViewVariablesPathReq
{
}

internal sealed class MsgViewVariablesReadPathRes : MsgViewVariablesPathRes
{
    public MsgViewVariablesReadPathRes()
    {
    }

    public MsgViewVariablesReadPathRes(MsgViewVariablesReadPathReq req) : base(req)
    {
    }
}

internal sealed class MsgViewVariablesWritePathReq : MsgViewVariablesPathReqVal
{
}

internal sealed class MsgViewVariablesWritePathRes : MsgViewVariablesPathRes
{
    public MsgViewVariablesWritePathRes()
    {
    }

    public MsgViewVariablesWritePathRes(MsgViewVariablesWritePathReq req) : base(req)
    {
    }
}

internal sealed class MsgViewVariablesInvokePathReq : MsgViewVariablesPathReqVal
{
}

internal sealed class MsgViewVariablesInvokePathRes : MsgViewVariablesPathRes
{
    public MsgViewVariablesInvokePathRes()
    {
    }

    public MsgViewVariablesInvokePathRes(MsgViewVariablesInvokePathReq req) : base(req)
    {
    }
}

internal sealed class MsgViewVariablesListPathReq : MsgViewVariablesPathReq
{
    public VVListPathOptions Options { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        base.ReadFromBuffer(buffer, serializer);
        var length = buffer.ReadInt32();
        using var stream = buffer.ReadAlignedMemory(length);
        Options = serializer.Deserialize<VVListPathOptions>(stream);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        base.WriteToBuffer(buffer, serializer);
        var stream = new MemoryStream();
        serializer.Serialize(stream, Options);

        buffer.Write((int)stream.Length);
        buffer.Write(stream.AsSpan());
    }
}

internal sealed class MsgViewVariablesListPathRes : MsgViewVariablesPathRes
{
    public MsgViewVariablesListPathRes()
    {
    }

    public MsgViewVariablesListPathRes(MsgViewVariablesListPathReq req) : base(req)
    {
    }
}
