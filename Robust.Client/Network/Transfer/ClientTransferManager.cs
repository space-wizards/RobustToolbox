using System;
using System.IO;
using System.Threading.Tasks;
using Robust.Shared.Asynchronous;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages.Transfer;
using Robust.Shared.Network.Transfer;

namespace Robust.Client.Network.Transfer;

internal sealed class ClientTransferManager : BaseTransferManager, ITransferManager
{
    private readonly IClientNetManager _netManager;
    private BaseTransferImpl? _transferImpl;

    public event Action? ClientHandshakeComplete;

    internal ClientTransferManager(
        IClientNetManager netManager,
        ILogManager logManager,
        ITaskManager taskManager)
        : base(logManager, NetMessageAccept.Client, taskManager)
    {
        _netManager = netManager;
    }

    public Stream StartTransfer(INetChannel channel, TransferStartInfo startInfo)
    {
        if (_transferImpl == null)
            throw new InvalidOperationException("Not connected yet!");

        if (channel != _netManager.ServerChannel)
            throw new InvalidOperationException("Invalid channel!");

        return _transferImpl.StartTransfer(startInfo);
    }

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgTransferInit>(RxTransferInit, NetMessageAccept.Client | NetMessageAccept.Handshake);
        _netManager.RegisterNetMessage<MsgTransferAckInit>();
        _netManager.RegisterNetMessage<MsgTransferData>(RxTransferData, NetMessageAccept.Client | NetMessageAccept.Handshake);
    }

    private async void RxTransferInit(MsgTransferInit message)
    {
        BaseTransferImpl impl;
        if (message.HttpInfo is { } httpInfo)
        {
            impl = new ClientTransferImplWebSocket(httpInfo, Sawmill, this, message.MsgChannel);
        }
        else
        {
            impl = new TransferImplLidgren(Sawmill, message.MsgChannel, this, _netManager);
        }

        _transferImpl = impl;
        await _transferImpl.ClientInit(default);

        ClientHandshakeComplete?.Invoke();
    }

    private void RxTransferData(MsgTransferData message)
    {
        if (_transferImpl is not TransferImplLidgren lidgren)
        {
            message.MsgChannel.Disconnect("Not lidgren");
            return;
        }

        lidgren.ReceiveData(message);
    }

    public Task ServerHandshake(INetChannel channel)
    {
        throw new NotSupportedException();
    }
}
