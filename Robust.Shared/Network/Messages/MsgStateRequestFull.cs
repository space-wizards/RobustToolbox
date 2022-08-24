using Lidgren.Network;
using Robust.Shared.Timing;

#nullable disable

namespace Robust.Shared.Network.Messages;

public sealed class MsgStateRequestFull : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Entity;

    public GameTick Tick;

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        Tick = buffer.ReadGameTick();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        buffer.Write(Tick);
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
}
