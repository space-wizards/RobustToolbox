using System;
using System.IO;
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
            /// The final part of the string-exchange handshake. Sent by the
            /// server after the client requests an updated copy of the mapping.
            /// Contains the updated string mapping.
            /// </summary>
            [UsedImplicitly]
            private class MsgStrings : NetMessage
            {

                public MsgStrings(INetChannel ch)
                    : base($"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgStrings)}", MsgGroups.Core)
                {
                }

                /// <value>
                /// The raw bytes of the string mapping held by the server.
                /// </value>
                public byte[]? Package { get; set; }

                public override void ReadFromBuffer(NetIncomingMessage buffer)
                {
                    var l = buffer.ReadVariableInt32();
                    var success = buffer.ReadBytes(l, out var buf);
                    if (!success)
                    {
                        throw new InvalidDataException("Not all of the bytes were available in the message.");
                    }

                    Package = buf;
                }

                public override void WriteToBuffer(NetOutgoingMessage buffer)
                {
                    if (Package == null)
                    {
                        throw new InvalidOperationException("Package has not been specified.");
                    }

                    buffer.WriteVariableInt32(Package.Length);
                    var start = buffer.LengthBytes;
                    buffer.Write(Package);
                    var added = buffer.LengthBytes - start;
                    if (added != Package.Length)
                    {
                        throw new InvalidOperationException("Not all of the bytes were written to the message.");
                    }
                }

            }

        }

    }

}
