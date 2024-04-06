using Lidgren.Network;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgState : CompressedNetMessage
    {
        // Lidgren does not currently support unreliable messages above MTU.
        // Ideally we would peg this to the actual configured MTU instead of the default constant, but oh well...
        public const int ReliableThreshold = NetPeerConfiguration.kDefaultMTU - 20;

        // If a state is larger than this, compress it with deflate.
        public const int CompressionThreshold = 256;

        public override MsgGroups MsgGroup => MsgGroups.Entity;

        public GameState State;
        public ZStdCompressionContext CompressionContext;

        internal bool HasWritten;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            MsgSize = buffer.LengthBytes;
            var size = ReadCompressed(buffer, serializer, out State, true);
            State.PayloadSize = size;
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            WriteCompressed(buffer, serializer, State, CompressionThreshold, CompressionContext, true);
            HasWritten = true;
            MsgSize = buffer.LengthBytes;
        }

        /// <summary>
        ///     Whether this state message is large enough to warrant being sent reliably.
        ///     This is only valid after
        /// </summary>
        public bool ShouldSendReliably()
        {
            DebugTools.Assert(HasWritten, "Attempted to determine sending method before determining packet size.");
            return State.ForceSendReliably || MsgSize > ReliableThreshold;
        }

        public override NetDeliveryMethod DeliveryMethod
            => ShouldSendReliably() ? NetDeliveryMethod.ReliableUnordered : base.DeliveryMethod;
    }
}
