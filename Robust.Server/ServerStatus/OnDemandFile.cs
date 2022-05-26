using System;
using System.Buffers;
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
/// An attempt to mitigate the amount of RAM usage caused by ACZ-related operations.
/// The idea is that Dictionary<string, OnDemandFile> should become the standard interchange.
/// </summary>
internal abstract class OnDemandFile
{
    /// <summary>
    /// Length of the target file. Assumed to be cached.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Content of the target file. Assumed to not be cached.
    /// Length should be equal to above length.
    /// </summary>
    public byte[] Content
    {
        get
        {
            byte[] data = new byte[Length];
            ReadExact(data);
            return data;
        }
    }

    public OnDemandFile(long len)
    {
        Length = len;
    }

    public abstract void ReadExact(Span<byte> data);
}

internal sealed class OnDemandDiskFile : OnDemandFile
{
    private readonly string _diskPath;

    public OnDemandDiskFile(string fileName) : base(new FileInfo(fileName).Length)
    {
        _diskPath = fileName;
    }

    public override void ReadExact(Span<byte> data)
    {
        try
        {
            using (FileStream fs = File.OpenRead(_diskPath))
            {
                fs.ReadExact(data);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"During OnDemandDiskFile.ReadExact: {_diskPath} (expected length {Length}, span length {data.Length})", ex);
        }
    }
}

internal sealed class OnDemandZipArchiveEntryFile : OnDemandFile
{
    private readonly ZipArchiveEntry _entry;

    public OnDemandZipArchiveEntryFile(ZipArchiveEntry src) : base(src.Length)
    {
        _entry = src;
    }

    public override void ReadExact(Span<byte> data)
    {
        using (var stream = _entry.Open())
        {
            stream.ReadExact(data);
        }
    }
}

