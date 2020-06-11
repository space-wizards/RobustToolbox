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
            /// <summary>
            /// The client part of the string-exchange handshake, sent after the
            /// client receives the mapping hash. Tells the server if the client
            /// needs an updated copy of the mapping.
            /// </summary>
            /// <remarks>
            /// Also sent by the client after a new copy of the string mapping
            /// has been successfully received and loaded. In this case, the value
            /// of <see cref="NeedsStrings"/> is always <c>false</c>.
            /// </remarks>
            [UsedImplicitly]
            private class MsgClientHandshake : NetMessage
            {

                public MsgClientHandshake(INetChannel ch)
                    : base($"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgClientHandshake)}", MsgGroups.Core)
                {
                }

                /// <value>
                /// <c>true</c> if the client needs a new copy of the mapping,
                /// <c>false</c> otherwise.
                /// </value>
                public bool NeedsStrings { get; set; }

                public override void ReadFromBuffer(NetIncomingMessage buffer) => NeedsStrings = buffer.ReadBoolean();

                public override void WriteToBuffer(NetOutgoingMessage buffer) => buffer.Write(NeedsStrings);

            }

        }

    }

}
