using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables.Commands;

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

        SendMessage(msg, session?.Channel);
        return tsc.Task;
    }

    public Task WriteRemotePath(string path, string value, ICommonSession? session = null)
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

        SendMessage(msg, session?.Channel);
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

        SendMessage(msg, session?.Channel);
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

        SendMessage(msg, session?.Channel);
        return tsc.Task;
    }

    private async void ReadRemotePathRequest(MsgViewVariablesReadPathReq req)
    {
        if (!CheckPermissions(req.MsgChannel, ViewVariablesReadCommand.Comm))
        {
            SendMessage(new MsgViewVariablesReadPathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.NoAccess,
            }, req.MsgChannel);
            return;
        }

        if (_netMan.IsServer && TryGetSession(req.Session, out var session))
        {
            var value = await ReadRemotePath(req.Path, session);
            SendMessage(new MsgViewVariablesReadPathRes(req)
            {
                Response = new []{value ?? "null"}
            }, req.MsgChannel);
            return;
        }

        var val = ReadPathSerialized(req.Path);

        if (val == null)
        {
            SendMessage(new MsgViewVariablesReadPathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.NoObject,
            }, req.MsgChannel);
            return;
        }

        SendMessage(new MsgViewVariablesReadPathRes(req)
        {
            Response = new []{ val }
        }, req.MsgChannel);
    }

    private async void WriteRemotePathRequest(MsgViewVariablesWritePathReq req)
    {
        if (!CheckPermissions(req.MsgChannel, ViewVariablesWriteCommand.Comm))
        {
            _netMan.ServerSendMessage(new MsgViewVariablesWritePathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.NoAccess,
            }, req.MsgChannel);
            return;
        }

        if (_netMan.IsServer && TryGetSession(req.Session, out var session))
        {
            await WriteRemotePath(req.Path, req.Value ?? string.Empty, session);
            SendMessage(new MsgViewVariablesWritePathRes(req), req.MsgChannel);
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

    private async void InvokeRemotePathRequest(MsgViewVariablesInvokePathReq req)
    {
        if (!CheckPermissions(req.MsgChannel, ViewVariablesInvokeCommand.Comm))
        {
            _netMan.ServerSendMessage(new MsgViewVariablesInvokePathRes(req)
            {
                Path = req.Path, ResponseCode = ViewVariablesResponseCode.NoAccess,
            }, req.MsgChannel);
            return;
        }

        if (_netMan.IsServer && TryGetSession(req.Session, out var session))
        {
            var retVal = await InvokeRemotePath(req.Path, req.Value ?? string.Empty, session);
            SendMessage(new MsgViewVariablesInvokePathRes(req)
            {
                Response = new []{retVal ?? "null"}
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

        string val;

        try
        {
            val = SerializeValue(path.InvokeReturnType, value) ?? value?.ToString() ?? "null";
        }
        catch (Exception)
        {
            val = value?.ToString() ?? "null";
        }

        SendMessage(new MsgViewVariablesInvokePathRes(req)
        {
            Response = new []{val},
        }, req.MsgChannel);
    }

    private async void ListRemotePathRequest(MsgViewVariablesListPathReq req)
    {
        if (!CheckPermissions(req.MsgChannel, "vv"))
        {
            _netMan.ServerSendMessage(new MsgViewVariablesListPathRes(req)
            {
                ResponseCode = ViewVariablesResponseCode.NoAccess,
            }, req.MsgChannel);
            return;
        }

        if (_netMan.IsServer && TryGetSession(req.Session, out var session))
        {
            var response = await ListRemotePath(req.Path, req.Options, session);
            SendMessage(new MsgViewVariablesListPathRes(req)
            {
                Response = response.ToArray(),
            }, req.MsgChannel);
            return;
        }

        var enumerable = ListPath(req.Path, req.Options)
            .OrderByDescending(p => p.StartsWith(req.Path))
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
        {
            tsc.TrySetResult(null); // TODO: Use exceptions
            return;
        }

        if (res.Response.Length == 0)
        {
            tsc.TrySetResult(null);
            return;
        }

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
        {
            tsc.TrySetResult(null); // TODO: Use exceptions
            return;
        }

        if (res.Response.Length == 0)
        {
            tsc.TrySetResult(null);
            return;
        }

        tsc.TrySetResult(res.Response[0]);
    }

    private void ListRemotePathResponse(MsgViewVariablesListPathRes res)
    {
        if (!_listRequests.Remove(res.RequestId, out var tsc))
            return;

        if (res.ResponseCode != ViewVariablesResponseCode.Ok)
        {
            tsc.TrySetResult(Enumerable.Empty<string>()); // TODO: Use exceptions
            return;
        }

        tsc.TrySetResult(res.Response);
    }

    private void SendMessage(NetMessage msg, INetChannel? channel = null)
    {
        // I'm surprised this isn't a method in INetManager already...
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

    protected abstract bool CheckPermissions(INetChannel channel, string command);
    protected abstract bool TryGetSession(Guid guid, [NotNullWhen(true)] out ICommonSession? session);
}
