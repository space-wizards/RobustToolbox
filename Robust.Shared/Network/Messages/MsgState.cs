using System;
using System.Buffers;
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
        public MemoryStream StateStream;

        public ZStdCompressionContext CompressionContext;

        internal bool HasWritten;

        private byte[] _compressed;
        private int _compressedLength;

        internal bool ForceSendReliably;

        public override int EstimateBufferSize()
        {
            var uncompressedLength = (int) StateStream.Length;

            int payloadLength;
            int compressedLength;
            if (_compressed != null)
            {
                payloadLength = _compressedLength;
                compressedLength = _compressedLength;
            }
            else if (uncompressedLength > CompressionThreshold)
            {
                payloadLength = ZStd.CompressBound(uncompressedLength);
                compressedLength = payloadLength;
            }
            else
            {
                payloadLength = uncompressedLength;
                compressedLength = 0;
            }

            // Message ID + two variable-length size prefixes + payload.
            return 1
                + VariableInt32Size(uncompressedLength)
                + VariableInt32Size(compressedLength)
                + payloadLength;
        }

        public void Compress()
        {
            if (_compressed != null || StateStream.Length <= CompressionThreshold)
                return;

            StateStream.Position = 0;
            _compressed = ArrayPool<byte>.Shared.Rent(ZStd.CompressBound((int) StateStream.Length));
            _compressedLength = CompressionContext.Compress2(_compressed, StateStream.AsSpan());
        }

        public void ReleaseCompressed()
        {
            if (_compressed == null)
                return;

            ArrayPool<byte>.Shared.Return(_compressed);
            _compressed = null;
        }

        private static int VariableInt32Size(int value)
        {
            // Look at NetSerializer if you want to understand the zigzag.
            var zigzag = (uint) ((value << 1) ^ (value >> 31));
            var size = 1;

            while (zigzag >= 0x80)
            {
                zigzag >>= 7;
                size++;
            }

            return size;
        }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            MsgSize = buffer.LengthBytes;
            var uncompressedLength = buffer.ReadVariableInt32();
            var compressedLength = buffer.ReadVariableInt32();
            MemoryStream finalStream;

            // State is compressed.
            if (compressedLength > 0)
            {
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
            buffer.WriteVariableInt32((int)StateStream.Length);

            // We compress the state.
            if (StateStream.Length > CompressionThreshold)
            {
                Compress();

                buffer.WriteVariableInt32(_compressedLength);
                buffer.Write(_compressed.AsSpan(0, _compressedLength));

                ReleaseCompressed();
            }
            // The state is sent as is.
            else
            {
                // 0 means that the state isn't compressed.
                buffer.WriteVariableInt32(0);
                buffer.Write(StateStream.AsSpan());
            }

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
