using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Robust.Shared.Upload;

/// <summary>
///     Sent by the client after it has applied a prototype dump.
/// </summary>
internal sealed class GamePrototypeLoadAckMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
    }
}
