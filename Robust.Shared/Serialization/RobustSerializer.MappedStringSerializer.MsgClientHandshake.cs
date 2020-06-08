using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Network;

namespace Robust.Shared.Serialization
{

    public partial class RobustSerializer
    {

        public partial class MappedStringSerializer
        {

            [UsedImplicitly]
            private class MsgClientHandshake : NetMessage
            {

                public MsgClientHandshake(INetChannel ch)
                    : base($"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgClientHandshake)}", MsgGroups.Core)
                {
                }

                public bool NeedsStrings { get; set; }

                public override void ReadFromBuffer(NetIncomingMessage buffer) => NeedsStrings = buffer.ReadBoolean();

                public override void WriteToBuffer(NetOutgoingMessage buffer) => buffer.Write(NeedsStrings);

            }

        }

    }

}
