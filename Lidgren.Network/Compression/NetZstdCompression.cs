using System;
using ImpromptuNinjas.ZStd;

namespace Lidgren.Network.Compression
{
    public class NetZstdCompression : NetCompression
    {
        private ZStdCompressor _compressor = new ZStdCompressor();
        private ZStdDecompressor _decompressor = new ZStdDecompressor();

        public override bool Compress(NetOutgoingMessage msg)
        {
            var compressBufferSize = (uint)CCtx.GetUpperBound((UIntPtr) msg.Data.Length);
            var compressBuffer = new byte[compressBufferSize];

            var size = (int)_compressor.Compress(compressBuffer, msg.Data);
            Array.Resize(ref compressBuffer, size);
            msg.Data = compressBuffer;
            msg.LengthBytes = msg.Data.Length;
            msg.LengthBits = msg.LengthBytes * 8;
            return true;
        }

        public override bool Decompress(NetIncomingMessage msg)
        {
            var decompressBufferSize = (uint)DCtx.GetUpperBound(msg.Data);
            var decompressBuffer = new byte[decompressBufferSize];

            var size = (int)_decompressor.Decompress(decompressBuffer, msg.Data);
            Array.Resize(ref decompressBuffer, size);
            msg.Data = decompressBuffer;
            msg.LengthBytes = msg.Data.Length;
            msg.LengthBits = msg.LengthBytes * 8;
            return true;
        }
    }
}
