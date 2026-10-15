using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Robust.Shared.Upload;

/// <summary>
/// Sent client -> server to acknowledge completion of a file upload.
/// </summary>
internal sealed class NetworkResourceAckMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    public int Key;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Key = buffer.ReadInt32();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Key);
    }
}
