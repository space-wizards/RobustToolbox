using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Robust.Shared.ViewVariables;

internal abstract partial class ViewVariablesManager
{
    internal const int MaxListPathResponseLength = 500;

    private uint _nextReadRequestId = 0;
    private uint _nextWriteRequestId = 0;
    private uint _nextInvokeRequestId = 0;
    private uint _nextListRequestId = 0;

    private readonly Dictionary<uint, TaskCompletionSource<(string?, ViewVariablesResponseCode)>> _readRequests = new();
    private readonly Dictionary<uint, TaskCompletionSource<(string?, ViewVariablesResponseCode)>> _writeRequests = new();
    private readonly Dictionary<uint, TaskCompletionSource<(string?, ViewVariablesResponseCode)>> _invokeRequests = new();
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

    public Task<(string?, ViewVariablesResponseCode)> ReadRemotePath(string path, ICommonSession? session = null)
    {
        if (!_netMan.IsConnected || (_netMan.IsServer && session == null))
            return Task.FromResult<(string?, ViewVariablesResponseCode)>((null, ViewVariablesResponseCode.InvalidRequest));

        var msg = new MsgViewVariablesReadPathReq()
        {
            RequestId = unchecked(_nextReadRequestId++),
            Path = path,
            Session = session?.UserId ?? Guid.Empty,
        };

        var tsc = new TaskCompletionSource<(string?, ViewVariablesResponseCode)>();
        _readRequests.Add(msg.RequestId, tsc);

        SendMessage(msg, session?.ConnectedClient);
        return tsc.Task;
    }

    public Task<(string?, ViewVariablesResponseCode)> WriteRemotePath(string path, string value, ICommonSession? session = null)
    {
        if (!_netMan.IsConnected || (_netMan.IsServer && session == null))
            return Task.FromResult<(string?, ViewVariablesResponseCode)>((null, ViewVariablesResponseCode.InvalidRequest));

        var msg = new MsgViewVariablesWritePathReq()
        {
            RequestId = unchecked(_nextWriteRequestId++),
            Path = path,
            Value = value,
            Session = session?.UserId ?? Guid.Empty,
        };

        var tsc = new TaskCompletionSource<(string?, ViewVariablesResponseCode)>();
        _writeRequests.Add(msg.RequestId, tsc);

        SendMessage(msg, session?.ConnectedClient);
        return tsc.Task;
    }

    public Task<(string?, ViewVariablesResponseCode)> InvokeRemotePath(string path, string arguments, ICommonSession? session = null)
    {
        if (!_netMan.IsConnected || (_netMan.IsServer && session == null))
            return Task.FromResult<(string?, ViewVariablesResponseCode)>((null, ViewVariablesResponseCode.InvalidRequest));

        var msg = new MsgViewVariablesInvokePathReq()
        {
            RequestId = unchecked(_nextInvokeRequestId++),
            Path = path,
            Value = arguments,
            Session = session?.UserId ?? Guid.Empty,
        };

        var tsc = new TaskCompletionSource<(string?, ViewVariablesResponseCode)>();
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

    private async void ReadRemotePathRequest(MsgViewVariablesReadPathReq req)
    {
        if (!CheckPermissions(req.MsgChannel))
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
                Response = new []{value.Item1 ?? "null" },
                ResponseCode = value.Item2,

            }, req.MsgChannel);
            return;
        }

        if (!TryReadPathSerialized(req.Path, out var val, out var error) || val == null)
        {
            SendMessage(new MsgViewVariablesReadPathRes(req)
            {
                Response = error == null ? Array.Empty<string>() : new[] { error }, 
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
        if (!CheckPermissions(req.MsgChannel))
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

        if (!TryResolvePath(req.Path, out var path, out var error))
        {
            SendMessage(new MsgViewVariablesWritePathRes(req)
            {
                Response = error == null ? Array.Empty<string>(): new[] { error },
                ResponseCode = ViewVariablesResponseCode.NoObject,
            }, req.MsgChannel);
            return;
        }

        object? value = null;
        if (req.Value != null && !TryDeserializeValue(path.Type, req.Value, out value, out error))
        {
            SendMessage(new MsgViewVariablesWritePathRes(req)
            {
                Response = error == null ? Array.Empty<string>() : new[] { error },
                ResponseCode = ViewVariablesResponseCode.ParseFailure,
            }, req.MsgChannel);
            return;
        }

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
        if (!CheckPermissions(req.MsgChannel))
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
                Response = new []{retVal.Item1 ?? "null"},
                ResponseCode = retVal.Item2
            }, req.MsgChannel);
            return;
        }

        ;

        if (!TryResolvePath(req.Path, out var path, out var error))
        {
            SendMessage(new MsgViewVariablesInvokePathRes(req)
            {
                Response = error == null ? Array.Empty<string>() : new[] { error }, 
                ResponseCode = ViewVariablesResponseCode.NoObject,
            }, req.MsgChannel);
            return;
        }

        var args = req.Value != null ? ParseArguments(req.Value) : Array.Empty<string>();

        if (!TryDeserializeArguments(path.InvokeParameterTypes, (int)path.InvokeOptionalParameters, args, out var desArgs, out error))
        {
            SendMessage(new MsgViewVariablesInvokePathRes(req)
            {
                Response = error == null ? Array.Empty<string>() : new[] { error },
                ResponseCode = ViewVariablesResponseCode.ParseFailure,
            }, req.MsgChannel);
            return;
        }

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
        if (!CheckPermissions(req.MsgChannel))
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

        if (res.Response.Length == 0)
        {
            tsc.TrySetResult((null, res.ResponseCode));
            return;
        }

        tsc.TrySetResult((res.Response[0], res.ResponseCode));
    }

    private void WriteRemotePathResponse(MsgViewVariablesWritePathRes res)
    {
        if (_writeRequests.Remove(res.RequestId, out var tsc))
            tsc.TrySetResult((null, res.ResponseCode));
    }

    private void InvokeRemotePathResponse(MsgViewVariablesInvokePathRes res)
    {
        if (!_invokeRequests.Remove(res.RequestId, out var tsc))
            return;

        if (res.Response.Length == 0)
        {
            tsc.TrySetResult((null, res.ResponseCode));
            return;
        }

        tsc.TrySetResult((res.Response[0], res.ResponseCode));
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

    protected abstract bool CheckPermissions(INetChannel channel);
    protected abstract bool TryGetSession(Guid guid, [NotNullWhen(true)] out ICommonSession? session);
}
