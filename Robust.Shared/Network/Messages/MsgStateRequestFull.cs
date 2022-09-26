using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

#nullable disable

namespace Robust.Shared.Network.Messages;

public sealed class MsgStateRequestFull : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Entity;

    public GameTick Tick;

    public EntityUid MissingEntity;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Tick = buffer.ReadGameTick();
        MissingEntity = buffer.ReadEntityUid();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Tick);
        buffer.Write(MissingEntity);
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
}
