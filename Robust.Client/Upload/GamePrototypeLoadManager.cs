using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Upload;

namespace Robust.Client.Upload;

public sealed partial class GamePrototypeLoadManager : SharedPrototypeLoadManager
{
    // Strict decoder: reject malformed UTF-8 instead of silently replacing it.
    private static readonly UTF8Encoding Utf8 = new(false, true);

    [Dependency] private ITaskManager _taskManager = default!;

    public override void SendGamePrototype(string prototype)
    {
        NetManager.ClientSendMessage(new GamePrototypeLoadMessage { PrototypeData = prototype });
    }

    protected override void OnPrototypeDumpReceived(TransferReceivedEvent ev)
    {
        ReadDump(ev);
    }

    private async void ReadDump(TransferReceivedEvent ev)
    {
        try
        {
            await using var stream = ev.DataStream;

            if (ev.Channel != ((IClientNetManager) NetManager).ServerChannel)
                return;

            var header = new byte[PrototypeDumpFormat.LengthSize];
            await stream.ReadExactlyAsync(header).ConfigureAwait(false);
            var ackKey = BinaryPrimitives.ReadInt32LittleEndian(header);

            await stream.ReadExactlyAsync(header).ConfigureAwait(false);
            var count = BinaryPrimitives.ReadInt32LittleEndian(header);
            if (count < 0)
            {
                FailDump($"Invalid dump prototype count {count}");
                return;
            }

            var prototypes = new List<string>();
            for (var i = 0; i < count; i++)
            {
                await stream.ReadExactlyAsync(header).ConfigureAwait(false);

                var length = BinaryPrimitives.ReadInt32LittleEndian(header);
                if (length < 0)
                {
                    FailDump($"Invalid dump prototype length {length}");
                    return;
                }

                var data = new byte[length];
                await stream.ReadExactlyAsync(data).ConfigureAwait(false);
                prototypes.Add(Utf8.GetString(data));
            }

            _taskManager.RunOnMainThread(() =>
            {
                if (ev.Channel != ((IClientNetManager) NetManager).ServerChannel
                    || !NetManager.IsConnected)
                {
                    return;
                }

                if (!TryApplyPrototypeDump(prototypes))
                {
                    Disconnect("Failed to apply prototype dump");
                    return;
                }

                SendLoadAck(ackKey);
            });
        }
        catch (Exception e)
        {
            Sawmill.Error($"Failed to receive prototype dump:\n{e}");
            Disconnect($"Prototype dump error: {e.Message}");
        }
    }

    private void SendLoadAck(int ackKey)
    {
        NetManager.ClientSendMessage(new NetworkResourceAckMessage { Key = ackKey });
    }

    private void FailDump(string reason)
    {
        Sawmill.Error($"{reason}. Disconnecting.");
        Disconnect($"Prototype dump error: {reason}");
    }

    private void Disconnect(string reason)
    {
        _taskManager.RunOnMainThread(() =>
            ((IClientNetManager) NetManager).ClientDisconnect(reason));
    }
}
