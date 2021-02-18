using System;
using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network.Messages
{

    /// <summary>
    /// The meat of the string-exchange handshake sandwich. Sent by the
    /// server after the client requests an updated copy of the mapping.
    /// Contains the updated string mapping.
    /// </summary>
    /// <seealso cref="RobustMappedStringSerializer.NetworkInitialize"/>
    [UsedImplicitly]
    internal class MsgMapStrStrings : NetMessage
    {

        public MsgMapStrStrings(INetChannel ch)
            : base(nameof(MsgMapStrStrings), MsgGroups.Core)
        {
        }

        /// <value>
        /// The raw bytes of the string mapping held by the server.
        /// </value>
        public byte[]? Package { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var size = buffer.ReadVariableInt32();
            buffer.ReadBytes(Package = new byte[size]);
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
