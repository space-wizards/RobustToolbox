using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Robust.Server.ServerStatus;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages.Transfer;
using Robust.Shared.Network.Transfer;

namespace Robust.Server.Network.Transfer;

internal sealed class ServerTransferManager : BaseTransferManager, ITransferManager
{
    internal const string TransferApiUrl = "/rt_transfer_init";

    private readonly IConfigurationManager _cfg;
    private readonly IStatusHost _statusHost;
    private readonly IServerNetManager _netManager;

    private readonly Dictionary<NetUserId, Player> _onlinePlayers = new();

    internal ServerTransferManager(IConfigurationManager cfg, IStatusHost statusHost, IServerNetManager netManager, ILogManager logManager, ITaskManager taskManager)
        : base(logManager, NetMessageAccept.Server, taskManager)
    {
        _cfg = cfg;
        _statusHost = statusHost;
        _netManager = netManager;
    }

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgTransferInit>();
        _netManager.RegisterNetMessage<MsgTransferData>(RxTransferData, NetMessageAccept.Server | NetMessageAccept.Handshake);
        _netManager.RegisterNetMessage<MsgTransferAckInit>(RxTransferAckInit, NetMessageAccept.Server | NetMessageAccept.Handshake);

        _statusHost.AddHandler(HandleRequest);

        _netManager.Disconnect += NetManagerOnDisconnect;
    }

    private void RxTransferData(MsgTransferData message)
    {
        throw new NotImplementedException();
    }

    private void RxTransferAckInit(MsgTransferAckInit message)
    {
        throw new NotImplementedException();
    }

    public Stream StartTransfer(INetChannel channel, TransferStartInfo startInfo)
    {
        if (!_onlinePlayers.TryGetValue(channel.UserId, out var player))
            throw new InvalidOperationException("Player is not connected yet!");

        return player.Impl.StartTransfer(startInfo);
    }

    private async Task<bool> HandleRequest(IStatusHandlerContext context)
    {
        if (context.Url.AbsolutePath != TransferApiUrl)
            return false;

        if (!context.IsWebSocketRequest)
        {
            Sawmill.Verbose("HTTP request failed: not a websocket request");
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        if (!context.RequestHeaders.TryGetValue(TransferImplWebSocket.UserIdHeaderName, out var userIdValue)
            || userIdValue.Count != 1)
        {
            Sawmill.Verbose("HTTP request failed: missing RT-UserId");
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        if (!Guid.TryParse(userIdValue[0], out var userId))
        {
            Sawmill.Verbose($"HTTP request failed: UserID '{userId}' invalid");
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        if (!_onlinePlayers.TryGetValue(new NetUserId(userId), out var player))
        {
            Sawmill.Warning($"HTTP request failed: UserID '{userId}' not online");
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        if (player.Impl is not ServerTransferImplWebSocket serverWs)
        {
            Sawmill.Warning($"HTTP request failed: UserID '{userId}' is not using websocket transfer");
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        await serverWs.HandleApiRequest(new NetUserId(userId), context);
        return true;
    }

    public async Task ServerHandshake(INetChannel channel)
    {
        if (_onlinePlayers.ContainsKey(channel.UserId))
            throw new InvalidOperationException("We already have data for this user??");

        var transferHttpEnabled = _cfg.GetCVar(CVars.TransferHttp);

        BaseTransferImpl impl;
        if (transferHttpEnabled)
        {
            impl = new ServerTransferImplWebSocket(Sawmill, this, _cfg, _netManager, channel);
        }
        else
        {
            impl = new TransferImplLidgren(Sawmill, channel, this, _netManager);
        }

        var datum = new Player
        {
            Impl = impl,
        };

        _onlinePlayers.Add(channel.UserId, datum);

        await impl.ServerInit();
    }

    public event Action ClientHandshakeComplete
    {
        add { }
        remove { }
    }

    private void NetManagerOnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        if (!_onlinePlayers.Remove(e.Channel.UserId, out var player))
            return;

        Sawmill.Debug("Cleaning up connection for channel {Player} that disconnected", e.Channel);
        player.Impl.Dispose();
    }

    private sealed class Player
    {
        public required BaseTransferImpl Impl;
    }
}
