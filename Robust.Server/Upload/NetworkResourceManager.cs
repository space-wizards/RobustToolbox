using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Player;
using Robust.Shared.Upload;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Server.Upload;

public sealed class NetworkResourcesUploadedEvent
{
    public ICommonSession Session { get; }
    public ImmutableArray<(ResPath Relative, byte[] Data)> Files { get; }

    internal NetworkResourcesUploadedEvent(ICommonSession session, ImmutableArray<(ResPath, byte[])> files)
    {
        Session = session;
        Files = files;
    }
}

public sealed class NetworkResourceManager : SharedNetworkResourceManager
{
    internal const int AckInitial = 1;

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerNetManager _serverNetManager = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IConGroupController _controller = default!;

    [Obsolete("Use ResourcesUploaded instead")]
    public event Action<ICommonSession, NetworkResourceUploadMessage>? OnResourceUploaded;
    public event Action<NetworkResourcesUploadedEvent>? ResourcesUploaded;

    [ViewVariables] public bool Enabled { get; private set; } = true;
    [ViewVariables] public float SizeLimit { get; private set; }

    internal event Action<INetChannel, int>? AckReceived;

    internal override void Initialize()
    {
        base.Initialize();

        TransferManager.RegisterTransferMessage(TransferKeyNetworkDownload);
        TransferManager.RegisterTransferMessage(TransferKeyNetworkUpload, ReceiveUpload);

        _cfgManager.OnValueChanged(CVars.ResourceUploadingEnabled, value => Enabled = value, true);
        _cfgManager.OnValueChanged(CVars.ResourceUploadingLimitMb, value => SizeLimit = value, true);

        _serverNetManager.RegisterNetMessage<NetworkResourceAckMessage>(RxAck);
    }

    private void RxAck(NetworkResourceAckMessage message)
    {
        AckReceived?.Invoke(message.MsgChannel, message.Key);
    }

    private async void ReceiveUpload(TransferReceivedEvent transfer)
    {
        // Do not allow uploading any new resources if it has been disabled.
        // Note: Any resources uploaded before being disabled will still be kept and sent.
        if (!Enabled)
        {
            transfer.Channel.Disconnect("Resource upload not enabled.");
            return;
        }

        if (!_playerManager.TryGetSessionByChannel(transfer.Channel, out var session))
        {
            transfer.Channel.Disconnect("Not in-game");
            return;
        }

        if (!_controller.CanCommand(session, "uploadfile"))
        {
            transfer.Channel.Disconnect("Not authorized");
            return;
        }

        Sawmill.Verbose("Ingesting file uploads from {Session}", session);

        List<(ResPath Relative, byte[] Data)> ingested;
        await using (var stream = transfer.DataStream)
        {
            ingested = await IngestFileStream(stream);
        }

        Sawmill.Verbose("Ingesting file uploads complete, distributing...");

        foreach (var channel in _serverNetManager.Channels)
        {
            SendToPlayer(channel, ingested);
        }

#pragma warning disable CS0618 // Type or member is obsolete
        if (OnResourceUploaded != null)
        {
            foreach (var (relative, data) in ingested)
            {
                OnResourceUploaded?.Invoke(session, new NetworkResourceUploadMessage
                {
                    MsgChannel = session.Channel,
                    Data = data,
                    RelativePath = relative
                });
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete

        ResourcesUploaded?.Invoke(new NetworkResourcesUploadedEvent(session, [..ingested]));
    }

    protected override void ValidateUpload(uint size)
    {
        if (SizeLimit > 0f && size * BytesToMegabytes > SizeLimit)
            throw new Exception("File upload too large!");
    }

    internal bool SendToNewUser(INetChannel channel)
    {
        var allFiles = ContentRoot.GetAllFiles().ToList();
        if (allFiles.Count == 0)
            return false;

        SendToPlayer(channel, allFiles, AckInitial);
        return true;
    }

    private async void SendToPlayer(INetChannel channel, List<(ResPath Relative, byte[] Data)> files, int ack = 0)
    {
        await using var stream = TransferManager.StartTransfer(channel,
            new TransferStartInfo
            {
                MessageKey = TransferKeyNetworkDownload
            });

        var ackBytes = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(ackBytes, ack);
        await stream.WriteAsync(ackBytes);

        await WriteFileStream(stream, files);
    }
}
