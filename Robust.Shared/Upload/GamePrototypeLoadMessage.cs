using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Robust.Shared.Upload;

public sealed class GamePrototypeLoadMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    public string PrototypeData { get; set; } = string.Empty;

    /// <summary>
    ///     Whether this message is part of the accumulated dump sent to a connecting client.
    ///     Live admin uploads are not dumps and are applied directly.
    /// </summary>
    public bool IsDump { get; set; }

    /// <summary>
    ///     Total number of parts the dump was split into. Always 1 for live uploads.
    /// </summary>
    public int PartCount { get; set; } = 1;

    /// <summary>
    ///     Zero-based index of this part within the dump.
    /// </summary>
    public int PartIndex { get; set; }

    public bool IsLastPart => PartIndex >= PartCount - 1;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        PartCount = buffer.ReadVariableInt32();
        PartIndex = buffer.ReadVariableInt32();
        IsDump = buffer.ReadBoolean();
        buffer.ReadPadBits();
        PrototypeData = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(PartCount);
        buffer.WriteVariableInt32(PartIndex);
        buffer.Write(IsDump);
        buffer.WritePadBits();
        buffer.Write(PrototypeData);
    }
}
