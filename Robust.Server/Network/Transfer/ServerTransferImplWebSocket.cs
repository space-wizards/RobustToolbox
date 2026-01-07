using System;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Robust.Server.ServerStatus;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages.Transfer;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Utility;

namespace Robust.Server.Network.Transfer;

internal sealed class ServerTransferImplWebSocket : TransferImplWebSocket
{
    private readonly IConfigurationManager _cfg;
    private readonly INetManager _netManager;
    private readonly SemaphoreSlim _apiLock = new(1, 1);
    private readonly TaskCompletionSource _connectTcs = new();

    public byte[]? Key;

    public ServerTransferImplWebSocket(
        ISawmill sawmill,
        BaseTransferManager parent,
        IConfigurationManager cfg,
        INetManager netManager,
        INetChannel channel)
        : base(sawmill, parent, channel)
    {
        _cfg = cfg;
        _netManager = netManager;
    }

    public override Task ServerInit()
    {
        Key = RandomNumberGenerator.GetBytes(RandomKeyBytes);

        var uriBuilder = new UriBuilder(string.Concat(
            _cfg.GetCVar(CVars.TransferHttpEndpoint).TrimEnd("/"),
            ServerTransferManager.TransferApiUrl));

        uriBuilder.Scheme = uriBuilder.Scheme switch
        {
            "http" => "ws",
            "https" => "wss",
            _ => throw new InvalidOperationException($"Invalid API endpoint scheme: {uriBuilder.Scheme}")
        };

        var url = uriBuilder.ToString();

        Sawmill.Verbose($"Transfer API URL is '{url}'");

        var initMsg = new MsgTransferInit();
        initMsg.HttpInfo = (url, Key);

        _netManager.ServerSendMessage(initMsg, Channel);

        return _connectTcs.Task;
    }

    public override Task ClientInit(CancellationToken cancel)
    {
        throw new NotSupportedException();
    }

    public async Task HandleApiRequest(NetUserId userId, IStatusHandlerContext context)
    {
        using var _ = await _apiLock.WaitGuardAsync();

        if (Key == null)
        {
            Sawmill.Warning($"HTTP request failed: UserID '{userId}' tried to connect twice");
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return;
        }

        if (!context.RequestHeaders.TryGetValue(KeyHeaderName, out var keyValue) || keyValue is not [{ } keyValueStr])
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return;
        }

        var buf = new byte[RandomKeyBytes];

        if (!Convert.TryFromBase64String(keyValueStr, buf, out var written) || written != RandomKeyBytes)
        {
            Sawmill.Verbose("HTTP request failed: key is not valid base64 or wrong length");
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return;
        }

        if (!CryptographicOperations.FixedTimeEquals(buf, Key))
        {
            Sawmill.Warning("HTTP request failed: key is wrong");
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return;
        }

        Sawmill.Debug("Client connect to transfer WS channel: {UserId}", userId);

        WebSocket = await context.AcceptWebSocketAsync();

        // We've connected.
        // Clear key so this can't be reconnected to.
        Key = null;

        _connectTcs.TrySetResult();

        ReadThread();
    }

    public override void Dispose()
    {
        _connectTcs.TrySetCanceled();
    }
}
