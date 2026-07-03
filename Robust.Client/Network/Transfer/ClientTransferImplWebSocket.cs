using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;

namespace Robust.Client.Network.Transfer;

internal sealed class ClientTransferImplWebSocket : TransferImplWebSocket
{
    private readonly (string EndpointUrl, byte[] Key) _info;
    private readonly bool _slow;

    public ClientTransferImplWebSocket(
        (string EndpointUrl, byte[] Key) info,
        ISawmill sawmill,
        BaseTransferManager parent,
        INetChannel channel,
        bool slow)
        : base(sawmill, parent, channel)
    {
        _info = info;
        _slow = slow;
    }

    public override async Task ClientInit(CancellationToken cancel)
    {
        var clientWs = new ClientWebSocket();
        clientWs.Options.SetRequestHeader(KeyHeaderName, Convert.ToBase64String(_info.Key));
        clientWs.Options.SetRequestHeader(UserIdHeaderName, Channel.UserId.ToString());

        if (_slow)
            await Task.Delay(2000, cancel);

        await clientWs.ConnectAsync(new Uri(_info.EndpointUrl), cancel);

        WebSocket = clientWs;

        ReadThread();
    }

    public override Task ServerInit()
    {
        throw new NotSupportedException();
    }
}
