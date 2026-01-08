using System;
using System.IO;
using System.Threading.Tasks;

namespace Robust.Shared.Network.Transfer;

public sealed class TransferStartInfo
{
    public required string MessageKey;
}

public sealed class TransferReceivedEvent
{
    public readonly string Key;
    public readonly Stream DataStream;
    public readonly INetChannel Channel;

    internal TransferReceivedEvent(string key, INetChannel channel, Stream stream)
    {
        Key = key;
        DataStream = stream;
        Channel = channel;
    }
}

[NotContentImplementable]
public interface ITransferManager
{
    Stream StartTransfer(INetChannel channel, TransferStartInfo startInfo);

    void RegisterTransferMessage(
        string key,
        Action<TransferReceivedEvent>? rxCallback = null,
        NetMessageAccept accept = NetMessageAccept.Both);

    // Engine API.

    internal void Initialize();
    internal void FrameUpdate();
    internal Task ServerHandshake(INetChannel channel);
    internal event Action ClientHandshakeComplete;
}

