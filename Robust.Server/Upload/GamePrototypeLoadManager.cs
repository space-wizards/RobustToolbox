using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Robust.Server.Console;
using Robust.Server.Player;
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
///     Manages sending runtime-loaded prototypes from game staff to clients.
/// </summary>
public sealed partial class GamePrototypeLoadManager : SharedPrototypeLoadManager
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConGroupController _controller = default!;

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

    internal SendDumpResult SendToNewUser(INetChannel channel, int ackKey)
    {
        return SendDump(channel, LoadedPrototypes, ackKey);
    }

    internal SendDumpResult SendDump(INetChannel channel, IReadOnlyList<string> prototypes, int ackKey)
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
        SendDumpAsync(stream, channel, snapshot, ackKey);
        return SendDumpResult.Sent;
    }

    private async void SendDumpAsync(Stream stream, INetChannel channel, string[] prototypes, int ackKey)
    {
        try
        {
            await using (stream)
            {
                var header = new byte[PrototypeDumpFormat.LengthSize];
                BinaryPrimitives.WriteInt32LittleEndian(header, ackKey);
                await stream.WriteAsync(header).ConfigureAwait(false);

                BinaryPrimitives.WriteInt32LittleEndian(header, prototypes.Length);
                await stream.WriteAsync(header).ConfigureAwait(false);

                foreach (var prototype in prototypes)
                {
                    var bytes = Encoding.UTF8.GetBytes(prototype);
                    BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
                    await stream.WriteAsync(header).ConfigureAwait(false);
                    await stream.WriteAsync(bytes).ConfigureAwait(false);
                }
            }
        }
        catch (Exception e)
        {
            Sawmill.Error($"Failed to send prototype dump to {channel.RemoteEndPoint}:\n{e}");
            channel.Disconnect("Failed to send prototype dump.");
        }
    }
}
