using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network.Messages
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
    internal sealed class MsgMapStrClientHandshake : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.String;

        /// <value>
        /// <c>true</c> if the client needs a new copy of the mapping,
        /// <c>false</c> otherwise.
        /// </value>
        public bool NeedsStrings { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
            => NeedsStrings = buffer.ReadBoolean();

        public override void WriteToBuffer(NetOutgoingMessage buffer)
            => buffer.Write(NeedsStrings);
    }
}
