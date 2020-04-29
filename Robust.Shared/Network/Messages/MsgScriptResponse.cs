using System.IO;
using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Messages
{
    public class MsgScriptResponse : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgScriptResponse);

        public MsgScriptResponse(INetChannel channel) : base(NAME, GROUP)
        {
        }

        public int ScriptSession { get; set; }
        public bool WasComplete { get; set; }

        // Echo of the entered code with syntax highlighting applied.
        public FormattedMessage Echo { get; set; }
        public FormattedMessage Response { get; set; }

        #endregion

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ScriptSession = buffer.ReadInt32();
            WasComplete = buffer.ReadBoolean();

            if (WasComplete)
            {
                var serializer = IoCManager.Resolve<IRobustSerializer>();

                var length = buffer.ReadVariableInt32();
                var stateData = buffer.ReadBytes(length);

                using var memoryStream = new MemoryStream(stateData);
                Echo = serializer.Deserialize<FormattedMessage>(memoryStream);
                Response = serializer.Deserialize<FormattedMessage>(memoryStream);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ScriptSession);
            buffer.Write(WasComplete);

            if (WasComplete)
            {
                var serializer = IoCManager.Resolve<IRobustSerializer>();

                var memoryStream = new MemoryStream();
                serializer.Serialize(memoryStream, Echo);
                serializer.Serialize(memoryStream, Response);

                buffer.WriteVariableInt32((int)memoryStream.Length);
                buffer.Write(memoryStream.ToArray());
            }
        }
    }
}
