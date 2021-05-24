using System.IO;
using Lidgren.Network;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgScriptResponse : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public int ScriptSession { get; set; }
        public bool WasComplete { get; set; }

        // Echo of the entered code with syntax highlighting applied.
        public FormattedMessage Echo;
        public FormattedMessage Response;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ScriptSession = buffer.ReadInt32();
            WasComplete = buffer.ReadBoolean();

            if (WasComplete)
            {
                var serializer = IoCManager.Resolve<IRobustSerializer>();

                buffer.ReadPadBits();
                var length = buffer.ReadVariableInt32();
                using var stream = buffer.ReadAlignedMemory(length);
                serializer.DeserializeDirect(stream, out Echo);
                serializer.DeserializeDirect(stream, out Response);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ScriptSession);
            buffer.Write(WasComplete);

            if (WasComplete)
            {
                buffer.WritePadBits();
                var serializer = IoCManager.Resolve<IRobustSerializer>();

                var memoryStream = new MemoryStream();
                serializer.SerializeDirect(memoryStream, Echo);
                serializer.SerializeDirect(memoryStream, Response);

                buffer.WriteVariableInt32((int)memoryStream.Length);
                memoryStream.TryGetBuffer(out var segment);
                buffer.Write(segment);
            }
        }
    }
}
