using System;
using System.Buffers;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Network;

public abstract class CompressedNetMessage : NetMessage
{
    protected int ReadCompressed<T>(NetIncomingMessage buffer, IRobustSerializer serializer, out T obj, bool direct)
    {
        var uncompressedLength = buffer.ReadVariableInt32();
        using var finalStream = RobustMemoryManager.GetMemoryStream(uncompressedLength);

        if (uncompressedLength >= 0)
        {
            // Object is uncompressed.
            buffer.ReadAlignedMemory(finalStream, uncompressedLength);
            if (direct)
                serializer.DeserializeDirect(finalStream, out obj);
            else
                obj = serializer.Deserialize<T>(finalStream);
            return uncompressedLength;
        }

        // Negative uncompressed length implies that the object is compressed.
        uncompressedLength = -uncompressedLength;
        var compressedLength = buffer.ReadVariableInt32();

        var stream = RobustMemoryManager.GetMemoryStream(compressedLength);
        buffer.ReadAlignedMemory(stream, compressedLength);
        using var decompressStream = new ZStdDecompressStream(stream);

        finalStream.SetLength(uncompressedLength);
        decompressStream.CopyTo(finalStream, uncompressedLength);
        finalStream.Position = 0;

        if (direct)
            serializer.DeserializeDirect(finalStream, out obj);
        else
            obj = serializer.Deserialize<T>(finalStream);

        return uncompressedLength;
    }

    protected int WriteCompressed<T>(
        NetOutgoingMessage buffer,
        IRobustSerializer serializer,
        T obj,
        int threshold,
        ZStdCompressionContext? ctx,
        bool direct)
        where T : notnull
    {
        using var stateStream = RobustMemoryManager.GetMemoryStream();
        if (direct)
            serializer.SerializeDirect(stateStream, obj);
        else
            serializer.Serialize(stateStream, obj);

        var uncompressedLength = (int) stateStream.Length;

        if (stateStream.Length <= threshold)
        {
            buffer.WriteVariableInt32(uncompressedLength);
            buffer.Write(stateStream.AsSpan());
            return uncompressedLength;
        }

        // Negative uncompressed length implies that the object is compressed.
        buffer.WriteVariableInt32(-uncompressedLength);
        stateStream.Position = 0;
        var buf = ArrayPool<byte>.Shared.Rent(ZStd.CompressBound((int)stateStream.Length));
        var length = ctx?.Compress2(buf, stateStream.AsSpan()) ?? ZStd.Compress(buf, stateStream.AsSpan());

        buffer.WriteVariableInt32(length);
        buffer.Write(buf.AsSpan(0, length));

        ArrayPool<byte>.Shared.Return(buf);
        return uncompressedLength;
    }
}
