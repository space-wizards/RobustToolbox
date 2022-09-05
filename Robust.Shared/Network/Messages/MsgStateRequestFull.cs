using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

#nullable disable

namespace Robust.Shared.Network.Messages;

public sealed class MsgStateRequestFull : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Entity;

    public GameTick Tick;

    public EntityUid MissingEntity;

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        Tick = buffer.ReadGameTick();
        MissingEntity = buffer.ReadEntityUid();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        buffer.Write(Tick);
        buffer.Write(MissingEntity);
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
}
