using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using Lidgren.Network;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgState : NetMessage
    {
        // If a state is large enough we send it ReliableUnordered instead.
        // This is to avoid states being so large that they consistently fail to reach the other end
        // (due to being in many parts).
        public const int ReliableThreshold = 1300;

        // If a state is larger than this, compress it with deflate.
        public const int CompressionThreshold = 256;

        public override MsgGroups MsgGroup => MsgGroups.Entity;

        public GameState State;
        public ZStdCompressionContext CompressionContext;

        private bool _hasWritten;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            MsgSize = buffer.LengthBytes;
            var uncompressedLength = buffer.ReadVariableInt32();
            var compressedLength = buffer.ReadVariableInt32();
            MemoryStream finalStream;

            // State is compressed.
            if (compressedLength > 0)
            {
                var stream = buffer.ReadAlignedMemory(compressedLength);
                using var decompressStream = new ZStdDecompressStream(stream);
                var decompressedStream = new MemoryStream(uncompressedLength);
                decompressStream.CopyTo(decompressedStream, uncompressedLength);
                decompressedStream.Position = 0;
                finalStream = decompressedStream;
            }
            // State is uncompressed.
            else
            {
                var stream = buffer.ReadAlignedMemory(uncompressedLength);
                finalStream = stream;
            }

            var serializer = IoCManager.Resolve<IRobustSerializer>();
            serializer.DeserializeDirect(finalStream, out State);
            finalStream.Dispose();

            State.PayloadSize = uncompressedLength;
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            var serializer = IoCManager.Resolve<IRobustSerializer>();
            var stateStream = new MemoryStream();
            serializer.SerializeDirect(stateStream, State);
            buffer.WriteVariableInt32((int)stateStream.Length);

            // We compress the state.
            if (stateStream.Length > CompressionThreshold)
            {
                var sw = Stopwatch.StartNew();
                stateStream.Position = 0;
                var buf = ArrayPool<byte>.Shared.Rent(ZStd.CompressBound((int)stateStream.Length));
                var length = CompressionContext.Compress2(buf, stateStream.AsSpan());

                buffer.WriteVariableInt32(length);

                buffer.Write(buf.AsSpan(0, length));

                var elapsed = sw.Elapsed;
                System.Console.WriteLine(
                    $"From: {State.FromSequence} To: {State.ToSequence} Size: {length} B Before: {stateStream.Length} B time: {elapsed}");

                ArrayPool<byte>.Shared.Return(buf);
            }
            // The state is sent as is.
            else
            {
                // 0 means that the state isn't compressed.
                buffer.WriteVariableInt32(0);

                buffer.Write(stateStream.AsSpan());
            }


            _hasWritten = false;
            MsgSize = buffer.LengthBytes;
        }

        /// <summary>
        ///     Whether this state message is large enough to warrant being sent reliably.
        ///     This is only valid after
        /// </summary>
        /// <returns></returns>
        public bool ShouldSendReliably()
        {
            // This check will be true in integration tests.
            // TODO: Maybe handle this better so that packet loss integration testing can be done?
            if (!_hasWritten)
            {
                return true;
            }

            return MsgSize > ReliableThreshold;
        }

        public override NetDeliveryMethod DeliveryMethod
        {
            get
            {
                if (ShouldSendReliably())
                {
                    return NetDeliveryMethod.ReliableUnordered;
                }

                return base.DeliveryMethod;
            }
        }
    }
}
