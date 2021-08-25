using System;
using System.IO;
using System.IO.Compression;
using Lidgren.Network;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgState : NetMessage
    {
        // If a state is large enough we send it ReliableUnordered instead.
        // This is to avoid states being so large that they consistently fail to reach the other end
        // (due to being in many parts).
        public const int ReliableThreshold = 1300;

        // If a state is larger than this, compress it with deflate.
        public const int CompressionThreshold = 256;

        public override MsgGroups MsgGroup => MsgGroups.Entity;

        public GameState State;

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
                using var decompressStream = new DeflateStream(stream, CompressionMode.Decompress);
                var decompressedStream = new MemoryStream(decompressStream.CopyToArray(), false);
                finalStream = decompressedStream;
                DebugTools.Assert(decompressedStream.Length == uncompressedLength);
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
            MemoryStream finalStream;
            var stateStream = new MemoryStream();
            DebugTools.Assert(stateStream.Length <= Int32.MaxValue);
            serializer.SerializeDirect(stateStream, State);
            buffer.WriteVariableInt32((int) stateStream.Length);

            // We compress the state.
            if (stateStream.Length > CompressionThreshold)
            {
                stateStream.Seek(0, SeekOrigin.Begin);
                var compressedStream = new MemoryStream();
                using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress, true))
                {
                    stateStream.CopyTo(deflateStream);
                }

                buffer.WriteVariableInt32((int) compressedStream.Length);
                finalStream = compressedStream;
            }
            // The state is sent as is.
            else
            {
                // 0 means that the state isn't compressed.
                buffer.WriteVariableInt32(0);
                finalStream = stateStream;
            }

            finalStream.TryGetBuffer(out var segment);
            buffer.Write(segment);
            finalStream.Dispose();

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
