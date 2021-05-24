using System;
using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network.Messages
{
    /// <summary>
    /// The server part of the string-exchange handshake. Sent as the
    /// first message in the handshake. Tells the client the hash of
    /// the current string mapping, so the client can check if it has
    /// a local copy.
    /// </summary>
    /// <seealso cref="RobustMappedStringSerializer.NetworkInitialize"/>
    [UsedImplicitly]
    internal class MsgMapStrServerHandshake : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Core;

        /// <value>
        /// The hash of the current string mapping held by the server.
        /// </value>
        public byte[]? Hash { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var len = buffer.ReadVariableInt32();
            if (len > 64)
            {
                throw new InvalidOperationException("Hash too long.");
            }

            buffer.ReadBytes(Hash = new byte[len]);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            if (Hash == null)
            {
                throw new InvalidOperationException("Package has not been specified.");
            }

            buffer.WriteVariableInt32(Hash.Length);
            buffer.Write(Hash);
        }
    }
}
