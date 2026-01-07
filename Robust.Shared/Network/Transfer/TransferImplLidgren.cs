using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Log;
using Robust.Shared.Network.Messages.Transfer;

namespace Robust.Shared.Network.Transfer;

internal sealed class TransferImplLidgren(
    ISawmill sawmill,
    INetChannel channel,
    BaseTransferManager transferManager,
    INetManager netManager) : BaseTransferImpl(sawmill, transferManager, channel)
{
    private TaskCompletionSource? _serverInitTcs;

    public override Task ServerInit()
    {
        var initMsg = new MsgTransferInit();

        netManager.ServerSendMessage(initMsg, Channel);

        _serverInitTcs = new TaskCompletionSource();
        return _serverInitTcs.Task;
    }

    public override Task ClientInit(CancellationToken cancel)
    {
        var initMsg = new MsgTransferAckInit();

        netManager.ClientSendMessage(initMsg);

        return Task.CompletedTask;
    }

    public override Stream StartTransfer(TransferStartInfo startInfo)
    {
        throw new NotImplementedException();
    }

    public void ReceiveInitAck()
    {
        _serverInitTcs?.TrySetResult();
    }
}
