using System;
using System.Buffers;
using System.IO;
using Lidgren.Network;
using Robust.Shared.Utility;

namespace Robust.Shared.Network;

internal readonly struct ZStdPayload : IDisposable
{
    public readonly int UncompressedLength;
    public readonly int CompressedLength;
    private readonly byte[] _buffer;

    private ZStdPayload(byte[] buffer, int uncompressedLength, int compressedLength)
    {
        _buffer = buffer;
        UncompressedLength = uncompressedLength;
        CompressedLength = compressedLength;
    }

    public bool IsCompressed => CompressedLength > 0;

    public ReadOnlySpan<byte> Data => _buffer.AsSpan(0, IsCompressed ? CompressedLength : UncompressedLength);

    public int EncodedSize => VariableInt32Size(UncompressedLength) + VariableInt32Size(CompressedLength) + Data.Length;

    public static ZStdPayload Create(MemoryStream stream, ZStdCompressionContext ctx, int threshold)
    {
        var length = (int) stream.Length;
        if (length <= threshold)
            return new ZStdPayload(stream.GetBuffer(), length, 0);

        var buffer = ArrayPool<byte>.Shared.Rent(ZStd.CompressBound(length));
        try
        {
            var compressedLength = ctx.Compress2(buffer, stream.AsSpan());
            return new ZStdPayload(buffer, length, compressedLength);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

    public void WriteTo(NetOutgoingMessage buffer)
    {
        buffer.WriteVariableInt32(UncompressedLength);
        buffer.WriteVariableInt32(CompressedLength);
        buffer.Write(Data);
    }

    public void Dispose()
    {
        if (IsCompressed)
            ArrayPool<byte>.Shared.Return(_buffer);
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
}
