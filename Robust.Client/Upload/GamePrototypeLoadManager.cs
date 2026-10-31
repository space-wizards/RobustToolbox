using System.Text;
using Robust.Shared.Network;
using Robust.Shared.Upload;

namespace Robust.Client.Upload;

public sealed class GamePrototypeLoadManager : SharedPrototypeLoadManager
{
    private readonly StringBuilder _dumpBuffer = new();
    private int _dumpPartCount = -1;
    private int _dumpNextIndex;

    public override void Initialize()
    {
        base.Initialize();
        NetManager.Disconnect += (_, _) => ResetDump();
    }

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

        if (message.PartCount <= 1)
        {
            TryLoadPrototypeData(message.PrototypeData);
            SendLoadAck();
            return;
        }

        if (message.PartIndex < 0 || message.PartIndex >= message.PartCount)
        {
            FailDump($"invalid part index {message.PartIndex} (count {message.PartCount})");
            return;
        }

        /* A dump may span multiple messages accumulate the parts and apply them once on the final
         part, as applying per part would trigger a full prototype and localization reload for every message.*/
        if (message.PartIndex == 0)
        {
            _dumpBuffer.Clear();
            _dumpPartCount = message.PartCount;
            _dumpNextIndex = 0;
        }
        else if (_dumpPartCount != message.PartCount)
        {
            FailDump($"part count changed mid-dump ({_dumpPartCount} -> {message.PartCount})");
            return;
        }

        if (message.PartIndex != _dumpNextIndex)
        {
            FailDump($"part {message.PartIndex} out of order (expected {_dumpNextIndex})");
            return;
        }

        if (_dumpNextIndex > 0)
            _dumpBuffer.Append("\n\n");

        _dumpBuffer.Append(message.PrototypeData);
        _dumpNextIndex++;

        if (!message.IsLastPart)
            return;

        var joined = _dumpBuffer.ToString();
        ResetDump();

        TryLoadPrototypeData(joined);
        SendLoadAck();
    }

    private void SendLoadAck()
    {
        NetManager.ClientSendMessage(new GamePrototypeLoadAckMessage());
    }

    private void FailDump(string reason)
    {
        ResetDump();

        Sawmill.Error($"Inconsistent prototype dump: {reason}. Disconnecting.");
        ((IClientNetManager) NetManager).ClientDisconnect($"Prototype dump error: {reason}");
    }

    private void ResetDump()
    {
        _dumpBuffer.Clear();
        _dumpPartCount = -1;
        _dumpNextIndex = 0;
    }
}
