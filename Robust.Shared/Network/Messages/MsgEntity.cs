using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Messages;

/// <summary>
/// NetMessage for sending networked ECS events (<see cref="EntityEventArgs"/>).
/// </summary>
public sealed class MsgEntity : CompressedNetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public EntityEventArgs Event;
    public uint Sequence;
    public GameTick SourceTick;
    private readonly int _threshold;
    private readonly ZStdCompressionContext? _ctx;

    public MsgEntity(
        EntityEventArgs ev,
        uint seq,
        GameTick sourceTick,
        int threshold,
        ZStdCompressionContext? ctx)
    {
        Event = ev;
        Sequence = seq;
        SourceTick = sourceTick;
        _threshold = threshold;
        _ctx = ctx;
    }

    public MsgEntity() : this(default!, default, default, int.MaxValue, default!)
    {
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        SourceTick = buffer.ReadGameTick();
        Sequence = buffer.ReadUInt32();
        ReadCompressed(buffer, serializer, out Event, false);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(SourceTick);
        buffer.Write(Sequence);
        WriteCompressed(buffer, serializer, Event, _threshold, _ctx, false);
    }

    public override string ToString()
    {
        return $"MsgEntity Comp, T: {SourceTick} S: {Sequence}, {Event}";
    }
}
