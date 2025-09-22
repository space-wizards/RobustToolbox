using System.IO;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgScriptResponse : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public int ScriptSession { get; set; }
        public bool WasComplete { get; set; }

        // Echo of the entered code with syntax highlighting applied.
        public FormattedMessage Echo;
        public FormattedMessage Response;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            ScriptSession = buffer.ReadInt32();
            WasComplete = buffer.ReadBoolean();

            if (WasComplete)
            {
                buffer.ReadPadBits();
                var length = buffer.ReadVariableInt32();
                using var stream = RobustMemoryManager.GetMemoryStream(length);
                buffer.ReadAlignedMemory(stream, length);
                serializer.DeserializeDirect(stream, out Echo);
                serializer.DeserializeDirect(stream, out Response);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(ScriptSession);
            buffer.Write(WasComplete);

            if (WasComplete)
            {
                buffer.WritePadBits();

                var memoryStream = new MemoryStream();
                serializer.SerializeDirect(memoryStream, Echo);
                serializer.SerializeDirect(memoryStream, Response);

                buffer.WriteVariableInt32((int)memoryStream.Length);
                buffer.Write(memoryStream.AsSpan());
            }
        }
    }
}
