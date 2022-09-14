using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

#nullable disable

namespace Robust.Shared.Network.Messages;

/// <summary>
///     Message containing a list of entities that have left a clients view.
/// </summary>
/// <remarks>
///     These messages are only sent if PVS is enabled. These messages are sent separately from the main game state.
/// </remarks>
public sealed class MsgStateLeavePvs : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Entity;

    public List<EntityUid> Entities;
    public GameTick Tick;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Tick = buffer.ReadGameTick();
        var length = buffer.ReadInt32();
        Entities = new(length);

        for (int i = 0; i < length; i++)
        {
            Entities.Add(buffer.ReadEntityUid());
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Tick);
        buffer.Write(Entities.Count);
        foreach (var ent in Entities)
        {
            buffer.Write(ent);
        }
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
}
