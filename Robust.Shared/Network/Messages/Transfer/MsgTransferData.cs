using System;
using System.Buffers;
using Lidgren.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network.Messages.Transfer;

internal sealed class MsgTransferData : NetMessage
{
    internal const NetDeliveryMethod Method = NetDeliveryMethod.ReliableOrdered;
    internal const int Channel = SequenceChannels.Transfer;

    public override NetDeliveryMethod DeliveryMethod => Method;
    public override int SequenceChannel => Channel;

    public ArraySegment<byte> Data;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();
        if (length > BaseTransferImpl.BufferSize)
            throw new Exception("Buffer size is too large");

        var arr = ArrayPool<byte>.Shared.Rent(length);
        buffer.ReadBytes(arr, 0, length);

        Data = new ArraySegment<byte>(arr, 0, length);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Data.Count);
        buffer.Write(Data.AsSpan());
    }
}
