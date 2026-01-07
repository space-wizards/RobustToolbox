using Lidgren.Network;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network.Messages.Transfer;

internal sealed class MsgTransferData : NetMessage
{
    public long StreamId;
    public bool Finished;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;
    public override int SequenceChannel => SequenceChannels.Transfer;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {

    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {

    }
}
