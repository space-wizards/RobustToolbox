using Lidgren.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network.Messages.Transfer;

internal sealed class MsgTransferInit : NetMessage
{
    public (string EndpointUrl, byte[] Key)? HttpInfo;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var httpAvailable = buffer.ReadBoolean();
        if (!httpAvailable)
        {
            HttpInfo = null;
            return;
        }

        buffer.SkipPadBits();
        var endpoint = buffer.ReadString();
        var key = buffer.ReadBytes(TransferImplWebSocket.RandomKeyBytes);

        HttpInfo = (endpoint, key);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        if (HttpInfo is null)
        {
            buffer.Write(false);
            return;
        }

        buffer.Write(true);
        buffer.WritePadBits();

        var (ep, key) = HttpInfo.Value;
        buffer.Write(ep);
        buffer.Write(key);
    }
}
