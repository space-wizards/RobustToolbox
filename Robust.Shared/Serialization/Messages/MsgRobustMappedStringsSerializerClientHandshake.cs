using System;
using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Network;

namespace Robust.Shared.Serialization
{

    /// <summary>
    /// The client part of the string-exchange handshake, sent after the
    /// client receives the mapping hash and after the client receives a
    /// strings package. Tells the server if the client needs an updated
    /// copy of the mapping.
    /// </summary>
    /// <remarks>
    /// Also sent by the client after a new copy of the string mapping
    /// has been received. If successfully loaded, the value of
    /// <see cref="NeedsStrings"/> is <c>false</c>, otherwise it will be
    /// <c>true</c>.
    /// </remarks>
    /// <seealso cref="RobustMappedStringSerializer.NetworkInitialize"/>
    [UsedImplicitly]
    internal class MsgRobustMappedStringsSerializerClientHandshake : NetMessage
    {

        public MsgRobustMappedStringsSerializerClientHandshake(INetChannel ch)
            : base(nameof(MsgRobustMappedStringsSerializerClientHandshake), MsgGroups.Core)
        {
        }

        /// <value>
        /// The hash of the types held by the server.
        /// </value>
        public byte[]? TypesHash { get; set; }

        /// <value>
        /// <c>true</c> if the client needs a new copy of the mapping,
        /// <c>false</c> otherwise.
        /// </value>
        public bool NeedsStrings { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var len = buffer.ReadVariableInt32();
            if (len > 64)
            {
                throw new InvalidOperationException("TypesHash too long.");
            }

            buffer.ReadBytes(TypesHash = new byte[len]);
            NeedsStrings = buffer.ReadBoolean();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {

            if (TypesHash == null)
            {
                throw new InvalidOperationException("TypesHash has not been specified.");
            }
            buffer.WriteVariableInt32(TypesHash.Length);
            buffer.Write(TypesHash);
            buffer.Write(NeedsStrings);
        }

    }

}
