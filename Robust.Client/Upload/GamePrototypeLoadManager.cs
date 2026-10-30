using System.Collections.Generic;
using Robust.Shared.Upload;

namespace Robust.Client.Upload;

public sealed class GamePrototypeLoadManager : SharedPrototypeLoadManager
{
    private readonly List<string> _dumpParts = new();

    public override void SendGamePrototype(string prototype)
    {
        NetManager.ClientSendMessage(new GamePrototypeLoadMessage { PrototypeData = prototype });
    }

    protected override void LoadPrototypeData(GamePrototypeLoadMessage message)
    {
        if (!message.IsDump)
        {
            base.LoadPrototypeData(message);
            return;
        }

        /* A dump may span multiple messages accumulate the parts and apply them once on the final
         part, as applying per part would trigger a full prototype and localization reload for every message.*/
        if (message.PartIndex == 0)
            _dumpParts.Clear();

        if (message.PartIndex != _dumpParts.Count)
        {
            Sawmill.Warning(
                $"Received prototype dump part {message.PartIndex} out of order (expected {_dumpParts.Count}); discarding partial dump.");
            _dumpParts.Clear();
            return;
        }

        _dumpParts.Add(message.PrototypeData);

        if (!message.IsLastPart)
            return;

        var joined = string.Join("\n\n", _dumpParts);
        _dumpParts.Clear();

        TryLoadPrototypeData(joined);
        NetManager.ClientSendMessage(new GamePrototypeLoadAckMessage());
    }
}
