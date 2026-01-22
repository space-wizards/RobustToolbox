using Lidgren.Network;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network.Messages.Transfer;

internal sealed class MsgTransferAckInit : NetMessage
{
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        // No data needed.
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        // No data needed.
    }
}
