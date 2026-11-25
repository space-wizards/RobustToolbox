using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Upload;

namespace Robust.Server.Upload;

internal enum SendDumpResult
{
    NoData,
    Sent,
    Failed,
}

/// <summary>
///     Handles server-side prototype uploads, limits, and dumps sent to clients.
/// </summary>
public sealed partial class GamePrototypeLoadManager : SharedPrototypeLoadManager
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConGroupController _controller = default!;
    [Dependency] private ITaskManager _taskManager = default!;

    internal event Action<string>? PrototypeLoaded;

    public override void SendGamePrototype(string prototype)
    {
        if (TryLoadPrototypeData(prototype))
            PrototypeLoaded?.Invoke(prototype);
    }

    protected override void LoadPrototypeData(GamePrototypeLoadMessage message)
    {
        var player = _playerManager.GetSessionByChannel(message.MsgChannel);
        if (_controller.CanCommand(player, "loadprototype"))
        {
            if (TryLoadPrototypeData(message.PrototypeData))
            {
                PrototypeLoaded?.Invoke(message.PrototypeData);
                Sawmill.Info($"Loaded adminbus prototype data from {player.Name}.");
            }
        }
        else
        {
            message.MsgChannel.Disconnect("Sent prototype message without permission!");
        }
    }

    protected override bool CanAcceptPrototype(string data)
    {
        if (!PrototypeDumpFormat.TryGetPayloadLength(LoadedPrototypes, out var currentBytes))
        {
            Sawmill.Error("Loaded prototype dump exceeds the wire format length limit.");
            return false;
        }

        var limit = GetPrototypeDumpMaxBytes();
        var size = PrototypeDumpFormat.GetPayloadSize(currentBytes, data);

        if (size > limit)
        {
            Sawmill.Error(
                $"Rejecting prototype upload: dump would be {size} bytes, limit is {limit} bytes.");

            return false;
        }

        return true;
    }

    internal SendDumpResult SendToNewUser(INetChannel channel, int ackKey, Action? onProgress = null)
    {
        return SendDump(channel, LoadedPrototypes, ackKey, onProgress);
    }

    internal SendDumpResult SendDump(
        INetChannel channel,
        IReadOnlyList<string> prototypes,
        int ackKey,
        Action? onProgress = null,
        bool sequential = false)
    {
        if (prototypes.Count == 0)
            return SendDumpResult.NoData;

        // Snapshot because LoadedPrototypes can change while the async send yields.
        var snapshot = new string[prototypes.Count];
        for (var i = 0; i < prototypes.Count; i++)
            snapshot[i] = prototypes[i];

        if (!channel.IsConnected)
        {
            Sawmill.Warning(
                $"Cannot send prototype dump to {channel.RemoteEndPoint}: channel is not connected.");

            return SendDumpResult.Failed;
        }

        Stream stream;
        try
        {
            stream = TransferManager.StartTransfer(
                channel,
                new TransferStartInfo { MessageKey = TransferKeyPrototypeDump });
        }
        catch (Exception e)
        {
            Sawmill.Error($"Failed to start prototype dump to {channel.RemoteEndPoint}:\n{e}");
            channel.Disconnect("Failed to start prototype dump.");
            return SendDumpResult.Failed;
        }

        // Exceptions are handled inside SendDumpAsync.
        SendDumpAsync(stream, channel, snapshot, ackKey, onProgress, sequential);
        return SendDumpResult.Sent;
    }

    private async void SendDumpAsync(
        Stream stream,
        INetChannel channel,
        string[] prototypes,
        int ackKey,
        Action? onProgress,
        bool sequential)
    {
        const int progressChunkSize = 16 * 1024;
        var remoteEndPoint = channel.RemoteEndPoint;

        try
        {
            await using (stream)
            {
                var header = new byte[PrototypeDumpFormat.LengthSize];
                BinaryPrimitives.WriteInt32LittleEndian(header, ackKey);
                await stream.WriteAsync(header).ConfigureAwait(false);
                onProgress?.Invoke();

                BinaryPrimitives.WriteInt32LittleEndian(header, prototypes.Length);
                await stream.WriteAsync(header).ConfigureAwait(false);
                onProgress?.Invoke();

                BinaryPrimitives.WriteInt32LittleEndian(header, sequential ? 1 : 0);
                await stream.WriteAsync(header).ConfigureAwait(false);
                onProgress?.Invoke();

                foreach (var prototype in prototypes)
                {
                    var bytes = Encoding.UTF8.GetBytes(prototype);
                    BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
                    await stream.WriteAsync(header).ConfigureAwait(false);
                    onProgress?.Invoke();

                    for (var offset = 0; offset < bytes.Length; offset += progressChunkSize)
                    {
                        var count = Math.Min(progressChunkSize, bytes.Length - offset);
                        await stream.WriteAsync(bytes.AsMemory(offset, count)).ConfigureAwait(false);
                        onProgress?.Invoke();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Sawmill.Error($"Failed to send prototype dump to {remoteEndPoint}:\n{e}");
            _taskManager.RunOnMainThread(() =>
            {
                if (channel.IsConnected)
                    channel.Disconnect("Failed to send prototype dump.");
            });
        }
    }
}
