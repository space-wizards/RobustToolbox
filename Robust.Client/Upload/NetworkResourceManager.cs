using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Upload;
using Robust.Shared.Utility;

namespace Robust.Client.Upload;

public sealed class NetworkResourceManager : SharedNetworkResourceManager
{
    [Dependency] private readonly IBaseClient _client = default!;

    internal override void Initialize()
    {
        base.Initialize();

        _client.RunLevelChanged += OnLevelChanged;

        TransferManager.RegisterTransferMessage(TransferKeyNetworkUpload);
        TransferManager.RegisterTransferMessage(TransferKeyNetworkDownload, ReceiveDownload);

        NetManager.RegisterNetMessage<NetworkResourceAckMessage>();
    }

    private async void ReceiveDownload(TransferReceivedEvent transfer)
    {
        Sawmill.Debug("Receiving file download transfer!");

        await using var stream = transfer.DataStream;

        try
        {
            var ackKeyBytes = new byte[4];
            await stream.ReadExactlyAsync(ackKeyBytes);
            var ackKey = BinaryPrimitives.ReadInt32LittleEndian(ackKeyBytes);

            await IngestFileStream(stream);

            if (ackKey != 0)
            {
                NetManager.ClientSendMessage(new NetworkResourceAckMessage
                {
                    Key = ackKey
                });
            }
        }
        catch (Exception e)
        {
            Sawmill.Error($"Error while downloading transfer resources: {e}");
        }
    }

    private void OnLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        // Clear networked resources when disconnecting from a multiplayer game.
        if (e.OldLevel == ClientRunLevel.InGame)
            ClearResources();
    }

    /// <summary>
    ///     Clears all the networked resources. If used while connected to a server, this will probably cause issues.
    /// </summary>
    public void ClearResources()
    {
        ContentRoot.Clear();
    }

    internal async void UploadResources(List<(ResPath Relative, byte[] Data)> files)
    {
        var clientNet = (IClientNetManager)NetManager;
        if (clientNet.ServerChannel is not { } channel)
            throw new InvalidOperationException("Not connected to server!");

        await using var transfer = TransferManager.StartTransfer(
            channel,
            new TransferStartInfo
            {
                MessageKey = TransferKeyNetworkUpload,
            });

        await WriteFileStream(transfer, files);
    }
}
