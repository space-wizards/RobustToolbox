using System;
using System.Text;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Upload;

namespace Robust.Server.Upload;

/// <summary>
///     Manages sending runtime-loaded prototypes from game staff to clients.
/// </summary>
public sealed partial class GamePrototypeLoadManager : SharedPrototypeLoadManager
{
    private static readonly byte[] Separator = { (byte) '\n', (byte) '\n' };

    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConGroupController _controller = default!;

    /// <summary>
    ///     Raised after a live prototype upload has been loaded and should be sent to clients.
    /// </summary>
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

    /// <summary>
    ///     Sends the currently loaded prototypes to a newly connected client.
    /// </summary>
    /// <param name="channel">The client's network channel.</param>
    /// <returns>Whether any data was sent, and thus an ack is expected.</returns>
    internal bool SendToNewUser(INetChannel channel)
    {
        if (LoadedPrototypes.Count == 0)
            return false;

        SendDumpAsync(channel, LoadedPrototypes.ToArray());
        return true;
    }

    private async void SendDumpAsync(INetChannel channel, string[] prototypes)
    {
        try
        {
            await using var stream = TransferManager.StartTransfer(
                channel,
                new TransferStartInfo { MessageKey = TransferKeyPrototypeDump });

            for (var i = 0; i < prototypes.Length; i++)
            {
                if (i > 0)
                    await stream.WriteAsync(Separator);

                await stream.WriteAsync(Encoding.UTF8.GetBytes(prototypes[i]));
            }
        }
        catch (Exception e)
        {
            Sawmill.Error($"Failed to send prototype dump to {channel.RemoteEndPoint}:\n{e}");
        }
    }
}
