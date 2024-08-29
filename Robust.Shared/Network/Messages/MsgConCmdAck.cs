using System.IO;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgConCmdAck : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.String;

        public FormattedMessage Text { get; set; }
        public bool Error { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            int length = buffer.ReadVariableInt32();
            using var stream = RobustMemoryManager.GetMemoryStream(length);
            buffer.ReadAlignedMemory(stream, length);
            Text = serializer.Deserialize<FormattedMessage>(stream);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            var stream = new MemoryStream();

            serializer.Serialize(stream, Text);

            buffer.WriteVariableInt32((int)stream.Length);
            buffer.Write(stream.AsSpan());
        }
    }
}
