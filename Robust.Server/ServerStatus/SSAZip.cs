using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Robust.Shared.Utility.Collections;
using SharpZstd.Interop;

namespace Robust.Server.ServerStatus;

/// <summary>
/// An attempt to create ZIP files with excessive speed.
/// NOTE: This is really an interim solution.
/// In particular the RAM usage of creating a ZIP this way is best described as "almost criminal".
/// We should really be dropping the ZIP-creation stuff entirely.
/// </summary>
internal static class SSAZip
{
    public static byte[] MakeZip(Dictionary<string, byte[]> files)
    {
        var entries = files.Select(pair => (Encoding.UTF8.GetBytes(pair.Key), pair.Value)).ToArray();
        return MakeZip(entries);
    }

    private static uint CRC32Content(byte[] data)
    {
        // it'll be fine
        return 0;
    }

    public static byte[] MakeZip((byte[] Key, byte[] Value)[] files)
    {
        // 22: size of EOCD
        // 30: size of LFH
        // 46: size of CDFH
        uint totalSize = 22;
        // this ends up at first CDFH, important for EOCD
        uint locationTracker = 0;
        uint cdSize = 0;
        Span<uint> locations = stackalloc uint[files.Length];
        Span<uint> crc = stackalloc uint[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            var pair = files[i];
            locations[i] = locationTracker;
            crc[i] = CRC32Content(pair.Value);
            uint localFile = (uint) (30 + pair.Key.Length + pair.Value.Length);
            locationTracker += localFile;
            uint centralDirectory = (uint) (localFile + 46 + pair.Key.Length);
            totalSize += centralDirectory;
            cdSize += centralDirectory;
        }
        // assumed to be zero-initialized
        byte[] result = new byte[totalSize];
        Span<byte> resultWriter = result;
        // LFH + file content
        for (int i = 0; i < files.Length; i++)
        {
            var pair = files[i];
            BinaryPrimitives.WriteInt32LittleEndian(resultWriter, 0x04034b50);
            // CRC32 skipped for now (can we afford to do this?)
            BinaryPrimitives.WriteUInt32LittleEndian(resultWriter.Slice(14), crc[i]);
            BinaryPrimitives.WriteInt32LittleEndian(resultWriter.Slice(18), pair.Value.Length);
            BinaryPrimitives.WriteInt32LittleEndian(resultWriter.Slice(22), pair.Value.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(resultWriter.Slice(26), (ushort) pair.Key.Length);
            resultWriter = resultWriter.Slice(30);
            // filename
            pair.Key.CopyTo(resultWriter);
            resultWriter = resultWriter.Slice(pair.Key.Length);
            // data
            pair.Value.CopyTo(resultWriter);
            resultWriter = resultWriter.Slice(pair.Value.Length);
        }
        // CDFH
        for (int i = 0; i < files.Length; i++)
        {
            var pair = files[i];
            BinaryPrimitives.WriteInt32LittleEndian(resultWriter, 0x02014b50);
            BinaryPrimitives.WriteUInt32LittleEndian(resultWriter.Slice(16), crc[i]);
            BinaryPrimitives.WriteInt32LittleEndian(resultWriter.Slice(20), pair.Value.Length);
            BinaryPrimitives.WriteInt32LittleEndian(resultWriter.Slice(24), pair.Value.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(resultWriter.Slice(28), (ushort) pair.Key.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(resultWriter.Slice(42), locations[i]);
            resultWriter = resultWriter.Slice(46);
            // filename
            pair.Key.CopyTo(resultWriter);
            resultWriter = resultWriter.Slice(pair.Key.Length);
        }
        // EOCD
        BinaryPrimitives.WriteInt32LittleEndian(resultWriter, 0x06054b50);
        BinaryPrimitives.WriteUInt16LittleEndian(resultWriter.Slice(8), (ushort) files.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(resultWriter.Slice(10), (ushort) files.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(resultWriter.Slice(12), cdSize);
        BinaryPrimitives.WriteUInt32LittleEndian(resultWriter.Slice(16), locationTracker);
        return result;
    }
}
