using System;
using Lidgren.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Robust.Shared.Network.Messages;

/// <summary>
/// Sent server -> client to synchronize initial TimeBase on connection.
/// Any further time synchronizations are done through the configuration system.
/// </summary>
internal sealed class MsgSyncTimeBase : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    public GameTick Tick;
    public TimeSpan Time;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Tick = buffer.ReadGameTick();
        Time = buffer.ReadTimeSpan();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Tick);
        buffer.Write(Time);
    }
}
