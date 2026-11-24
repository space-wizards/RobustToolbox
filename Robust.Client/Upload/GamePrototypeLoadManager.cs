using System;
using System.IO;
using System.Text;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Upload;

namespace Robust.Client.Upload;

public sealed partial class GamePrototypeLoadManager : SharedPrototypeLoadManager
{
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
            using var reader = new StreamReader(stream, Utf8, false, 1024, leaveOpen: true);
            var text = await reader.ReadToEndAsync();

            _taskManager.RunOnMainThread(() =>
            {
                TryLoadPrototypeData(text);
                SendLoadAck();
            });
        }
        catch (Exception e)
        {
            Sawmill.Error($"Failed to receive prototype dump:\n{e}");
            Disconnect($"Prototype dump error: {e.Message}");
        }
    }

    private void SendLoadAck()
    {
        NetManager.ClientSendMessage(new GamePrototypeLoadAckMessage());
    }

    private void Disconnect(string reason)
    {
        _taskManager.RunOnMainThread(() =>
            ((IClientNetManager) NetManager).ClientDisconnect(reason));
    }
}
