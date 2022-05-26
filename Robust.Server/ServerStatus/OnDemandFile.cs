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
internal interface OnDemandFile
{
    /// <summary>
    /// Length of the target file. Assumed to be cached.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Content of the target file. Assumed to not be cached.
    /// Length should be equal to above length.
    /// </summary>
    byte[] Content { get; }
}

internal sealed class OnDemandDiskFile : OnDemandFile
{
    private readonly string _diskPath;

    public long Length { get; }
    public byte[] Content
    {
        get
        {
            var data = File.ReadAllBytes(_diskPath);
            if (data.Length != Length)
            {
                throw new Exception($"Unexpected change in length of {_diskPath} from {Length} to {data.Length}!");
            }
            return data;
        }
    }

    public OnDemandDiskFile(string fileName)
    {
        _diskPath = fileName;
        Length = new FileInfo(fileName).Length;
    }
}

