using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lidgren.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.ViewVariables;

internal abstract partial class ViewVariablesManager
{
    internal const int MaxListPathResponseLength = 500;

    private uint _nextReadRequestId = 0;
    private uint _nextWriteRequestId = 0;
    private uint _nextInvokeRequestId = 0;
    private uint _nextListRequestId = 0;

    private readonly Dictionary<uint, TaskCompletionSource<string?>> _readRequests = new();
    private readonly Dictionary<uint, TaskCompletionSource> _writeRequests = new();
    private readonly Dictionary<uint, TaskCompletionSource<string?>> _invokeRequests = new();
    private readonly Dictionary<uint, TaskCompletionSource<IEnumerable<string>>> _listRequests = new();

    private void InitializeRemote()
    {
        _netMan.RegisterNetMessage<MsgViewVariablesReadPathReq>(ReadRemotePathRequest);
        _netMan.RegisterNetMessage<MsgViewVariablesWritePathReq>(WriteRemotePathRequest);
        _netMan.RegisterNetMessage<MsgViewVariablesInvokePathReq>(InvokeRemotePathRequest);
        _netMan.RegisterNetMessage<MsgViewVariablesListPathReq>(ListRemotePathRequest);

        _netMan.RegisterNetMessage<MsgViewVariablesReadPathRes>(ReadRemotePathResponse);
        _netMan.RegisterNetMessage<MsgViewVariablesWritePathRes>(WriteRemotePathResponse);
        _netMan.RegisterNetMessage<MsgViewVariablesInvokePathRes>(InvokeRemotePathResponse);
        _netMan.RegisterNetMessage<MsgViewVariablesListPathRes>(ListRemotePathResponse);
    }

    public Task<string?> ReadRemotePath(string path, ICommonSession? session = null)
    {
        if (!_netMan.IsConnected || (_netMan.IsServer && session == null))
            return Task.FromResult<string?>(null);

        var msg = new MsgViewVariablesReadPathReq()
        {
            RequestId = unchecked(_nextReadRequestId++),
            Path = path,
            Session = session?.UserId ?? Guid.Empty,
        };

        var tsc = new TaskCompletionSource<string?>();
        _readRequests.Add(msg.RequestId, tsc);

        SendMessage(msg, session?.ConnectedClient);
        return tsc.Task;
    }

    public Task WriteRemotePath(string path, string? value, ICommonSession? session = null)
    {
        if (!_netMan.IsConnected || (_netMan.IsServer && session == null))
            return Task.CompletedTask;

        var msg = new MsgViewVariablesWritePathReq()
        {
            RequestId = unchecked(_nextWriteRequestId++),
            Path = path,
            Value = value,
            Session = session?.UserId ?? Guid.Empty,
        };

        var tsc = new TaskCompletionSource();
        _writeRequests.Add(msg.RequestId, tsc);

        SendMessage(msg, session?.ConnectedClient);
        return tsc.Task;
    }

    public Task<string?> InvokeRemotePath(string path, string arguments, ICommonSession? session = null)
    {
        if (!_netMan.IsConnected || (_netMan.IsServer && session == null))
            return Task.FromResult<string?>(null);

        var msg = new MsgViewVariablesInvokePathReq()
        {
            RequestId = unchecked(_nextInvokeRequestId++),
            Path = path,
            Value = arguments,
            Session = session?.UserId ?? Guid.Empty,
        };

        var tsc = new TaskCompletionSource<string?>();
        _invokeRequests.Add(msg.RequestId, tsc);

        SendMessage(msg, session?.ConnectedClient);
        return tsc.Task;
    }

    public Task<IEnumerable<string>> ListRemotePath(string path, VVListPathOptions options, ICommonSession? session = null)
    {
        if (!_netMan.IsConnected || (_netMan.IsServer && session == null))
            return Task.FromResult(Enumerable.Empty<string>());

        var msg = new MsgViewVariablesListPathReq()
        {
            RequestId = unchecked(_nextListRequestId++),
            Path = path,
            Options = options,
            Session = session?.UserId ?? Guid.Empty,
        };

        var tsc = new TaskCompletionSource<IEnumerable<string>>();
        _listRequests.Add(msg.RequestId, tsc);

        SendMessage(msg, session?.ConnectedClient);
        return tsc.Task;
    }

    private void ReadRemotePathRequest(MsgViewVariablesReadPathReq req)
    {
        if (!CheckPermissions(req.MsgChannel))
        {
            SendMessage(new MsgViewVariablesReadPathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.NoAccess,
            }, req.MsgChannel);
            return;
        }

        var obj = ReadPath(req.Path);

        if (obj == null)
        {
            SendMessage(new MsgViewVariablesReadPathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.NoObject,
            }, req.MsgChannel);
            return;
        }

        SendMessage(new MsgViewVariablesReadPathRes(req)
        {
            Response = new []{ SerializeValue(obj.GetType(), obj) ?? "null" }
        }, req.MsgChannel);
    }

    private void WriteRemotePathRequest(MsgViewVariablesWritePathReq req)
    {
        if (!CheckPermissions(req.MsgChannel))
        {
            _netMan.ServerSendMessage(new MsgViewVariablesWritePathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.NoAccess,
            }, req.MsgChannel);
            return;
        }

        var path = ResolvePath(req.Path);

        if (path == null)
        {
            SendMessage(new MsgViewVariablesWritePathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.NoObject,
            }, req.MsgChannel);
            return;
        }

        var value = req.Value != null ? DeserializeValue(path.Type, req.Value) : null;

        try
        {
            path.Set(value);
        }
        catch (Exception)
        {
            SendMessage(new MsgViewVariablesWritePathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.InvalidRequest,
            }, req.MsgChannel);
            return;
        }

        SendMessage(new MsgViewVariablesWritePathRes(req), req.MsgChannel);
    }

    private void InvokeRemotePathRequest(MsgViewVariablesInvokePathReq req)
    {
        if (!CheckPermissions(req.MsgChannel))
        {
            _netMan.ServerSendMessage(new MsgViewVariablesInvokePathRes(req)
            {
                Path = req.Path, ResponseCode = ViewVariablesResponseCode.NoAccess,
            }, req.MsgChannel);
            return;
        }

        var path = ResolvePath(req.Path);

        if (path == null)
        {
            SendMessage(new MsgViewVariablesInvokePathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.NoObject,
            }, req.MsgChannel);
            return;
        }

        var args = req.Value != null ? ParseArguments(req.Value) : Array.Empty<string>();
        var desArgs = DeserializeArguments(path.InvokeParameterTypes, (int)path.InvokeOptionalParameters, args);
        object? value;

        try
        {
            value = path.Invoke(desArgs);
        }
        catch (Exception)
        {
            SendMessage(new MsgViewVariablesInvokePathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.InvalidRequest,
            }, req.MsgChannel);
            return;
        }

        SendMessage(new MsgViewVariablesInvokePathRes(req)
        {
            Response = new []{SerializeValue(path.InvokeReturnType, value) ?? "null"},
        }, req.MsgChannel);
    }

    private void ListRemotePathRequest(MsgViewVariablesListPathReq req)
    {
        if (!CheckPermissions(req.MsgChannel))
        {
            _netMan.ServerSendMessage(new MsgViewVariablesListPathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.NoAccess,
            }, req.MsgChannel);
            return;
        }

        var enumerable = ListPath(req.Path, req.Options)
            .OrderBy(p => p.StartsWith(req.Path))
            .Take(Math.Min(MaxListPathResponseLength, req.Options.RemoteListLength))
            .ToArray();

        SendMessage(new MsgViewVariablesListPathRes(req)
        {
            Response = enumerable,
        }, req.MsgChannel);
    }

    private void ReadRemotePathResponse(MsgViewVariablesReadPathRes res)
    {
        if (!_readRequests.Remove(res.RequestId, out var tsc))
            return;

        if (res.ResponseCode != ViewVariablesResponseCode.Ok)
            tsc.TrySetResult(null); // TODO: Use exceptions

        tsc.TrySetResult(res.Response[0]);
    }

    private void WriteRemotePathResponse(MsgViewVariablesWritePathRes res)
    {
        if (!_writeRequests.Remove(res.RequestId, out var tsc))
            return;

        // TODO: Use exceptions
        tsc.SetResult();
    }

    private void InvokeRemotePathResponse(MsgViewVariablesInvokePathRes res)
    {
        if (!_invokeRequests.Remove(res.RequestId, out var tsc))
            return;

        if (res.ResponseCode != ViewVariablesResponseCode.Ok)
            tsc.TrySetResult(null); // TODO: Use exceptions

        tsc.TrySetResult(res.Response[0]);
    }

    private void ListRemotePathResponse(MsgViewVariablesListPathRes res)
    {
        if (!_listRequests.Remove(res.RequestId, out var tsc))
            return;

        if (res.ResponseCode != ViewVariablesResponseCode.Ok)
            tsc.TrySetResult(Enumerable.Empty<string>()); // TODO: Use exceptions

        tsc.TrySetResult(res.Response);
    }

    private void SendMessage(NetMessage msg, INetChannel? channel = null)
    {
        if (_netMan.IsServer)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            _netMan.ServerSendMessage(msg, channel);
        }
        else
        {
            _netMan.ClientSendMessage(msg);
        }
    }

    protected abstract bool CheckPermissions(INetChannel channel);
}

internal abstract class MsgViewVariablesPath : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public uint RequestId { get; set; } = 0;
    public string Path { get; set; } = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        RequestId = buffer.ReadUInt32();
        Path = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        buffer.Write(RequestId);
        buffer.Write(Path);
    }
}

internal abstract class MsgViewVariablesPathReq : MsgViewVariablesPath
{
    public Guid Session { get; set; } = Guid.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        base.ReadFromBuffer(buffer);
        Session = buffer.ReadGuid();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        base.WriteToBuffer(buffer);
        buffer.Write(Session);
    }
}

internal abstract class MsgViewVariablesPathReqVal : MsgViewVariablesPathReq
{
    public string? Value { get; set; } = null;

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        base.ReadFromBuffer(buffer);
        Value = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        base.WriteToBuffer(buffer);
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

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        base.ReadFromBuffer(buffer);
        ResponseCode = (ViewVariablesResponseCode) buffer.ReadUInt16();
        var length = buffer.ReadInt32();
        Response = new string[length];

        for (var i = 0; i < length; i++)
        {
            Response[i] = buffer.ReadString();
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        base.WriteToBuffer(buffer);
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

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        base.ReadFromBuffer(buffer);
        var serializer = IoCManager.Resolve<IRobustSerializer>();
        var length = buffer.ReadInt32();
        using var stream = buffer.ReadAlignedMemory(length);
        Options = serializer.Deserialize<VVListPathOptions>(stream);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        base.WriteToBuffer(buffer);
        var serializer = IoCManager.Resolve<IRobustSerializer>();
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
