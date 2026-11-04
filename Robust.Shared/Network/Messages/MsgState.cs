using System.IO;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgState : NetMessage
    {
        // Lidgren does not currently support unreliable messages above MTU.
        // Ideally we would peg this to the actual configured MTU instead of the default constant, but oh well...
        public const int ReliableThreshold = NetPeerConfiguration.kDefaultMTU - 20;

        // If a state is larger than this, we will compress it
        // TODO PVS make this a cvar
        // TODO PVS figure out optimal value
        public const int CompressionThreshold = 256;

        public override MsgGroups MsgGroup => MsgGroups.Entity;

        public GameState State;

        internal ZStdPayload Payload;

        internal bool HasWritten;

        internal bool ForceSendReliably;

        public override int EstimateBufferSize() => 1 + Payload.EncodedSize;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            MsgSize = buffer.LengthBytes;
            var uncompressedLength = buffer.ReadVariableInt32();
            var compressedLength = buffer.ReadVariableInt32();
            MemoryStream finalStream;

            // State is compressed.
            if (compressedLength > 0)
            {
                buffer.AssertAtLeastSize(compressedLength);
                var stream = RobustMemoryManager.GetMemoryStream(compressedLength);
                buffer.ReadAlignedMemory(stream, compressedLength);

                using var decompressStream = new ZStdDecompressStream(stream);
                finalStream = RobustMemoryManager.GetMemoryStream(uncompressedLength);
                finalStream.SetLength(uncompressedLength);
                decompressStream.CopyTo(finalStream, uncompressedLength);
                finalStream.Position = 0;
            }
            // State is uncompressed.
            else
            {
                buffer.AssertAtLeastSize(uncompressedLength);
                finalStream = RobustMemoryManager.GetMemoryStream(uncompressedLength);
                buffer.ReadAlignedMemory(finalStream, uncompressedLength);
            }

            try
            {
                serializer.DeserializeDirect(finalStream, out State);
            }
            finally
            {
                finalStream.Dispose();
            }

            State.PayloadSize = uncompressedLength;
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            Payload.WriteTo(buffer);
            HasWritten = true;
            MsgSize = buffer.LengthBytes;
        }

        /// <summary>
        ///     Whether this state message is large enough to warrant being sent reliably.
        ///     This is only valid after
        /// </summary>
        /// <returns></returns>
        public bool ShouldSendReliably()
        {
            DebugTools.Assert(HasWritten, "Attempted to determine sending method before determining packet size.");
            return ForceSendReliably || MsgSize > ReliableThreshold;
        }

        public override NetDeliveryMethod DeliveryMethod => ShouldSendReliably()
                ? NetDeliveryMethod.ReliableUnordered
                : base.DeliveryMethod;
    }
}
