using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using SharpZstd.Interop;
using SpaceWizards.Sodium;

namespace Robust.Server.ServerStatus;

// Contains source logic for ACZ (Automatic Client Zip)
// This entails the following:
// * Automatic generation of client zip on development servers.
// * Loading of pre-built client zip on release servers. ("Hybrid ACZ")

internal sealed partial class StatusHost
{
    private (string binFolder, string[] assemblies)? _aczInfo;

    // -- Dictionary<string, OnDemandFile> methods --

    private Dictionary<string, OnDemandFile>? SourceAczDictionary()
    {
        return SourceAczDictionaryViaFile() ?? SourceAczDictionaryViaMagic();
    }

    private Dictionary<string, OnDemandFile>? SourceAczDictionaryViaFile()
    {
        var path = PathHelpers.ExecutableRelativeFile("Content.Client.zip");
        if (!File.Exists(path)) return null;
        _aczSawmill.Info($"StatusHost found client zip: {path}");
        // Note: We don't want to explicitly close this, as the OnDemandFiles will hold references to this.
        // Let it be cleaned up by GC eventually.
        FileStream fs = File.OpenRead(path);
        return SourceAczDictionaryViaZipStream(fs);
    }

    private Dictionary<string, OnDemandFile> SourceAczDictionaryViaZipStream(Stream stream)
    {
        var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var archive = new Dictionary<string, OnDemandFile>();
        foreach (var entry in zip.Entries)
        {
            // Ignore directory entries.
            if (entry.Name == "")
                continue;
            archive[entry.FullName] = new OnDemandZipArchiveEntryFile(entry);
        }
        return archive;
    }

    private Dictionary<string, OnDemandFile>? SourceAczDictionaryViaMagic()
    {
        var (binFolderPath, assemblyNames) =
            _aczInfo ?? ("Content.Client", new[] { "Content.Client", "Content.Shared" });

        var archive = new Dictionary<string, OnDemandFile>();

        foreach (var assemblyName in assemblyNames)
        {
            AttemptPullFromDisk($"Assemblies/{assemblyName}.dll", $"../../bin/{binFolderPath}/{assemblyName}.dll");
            AttemptPullFromDisk($"Assemblies/{assemblyName}.pdb", $"../../bin/{binFolderPath}/{assemblyName}.pdb");
        }

        var prefix = PathHelpers.ExecutableRelativeFile("../../Resources");
        foreach (var path in PathHelpers.GetFiles(prefix))
        {
            var relPath = Path.GetRelativePath(prefix, path);
            if (OperatingSystem.IsWindows())
                relPath = relPath.Replace('\\', '/');
            AttemptPullFromDisk(relPath, path);
        }

        return archive;

        void AttemptPullFromDisk(string pathTo, string pathFrom)
        {
            // _aczSawmill.Debug($"StatusHost PrepareACZMagic: {pathFrom} -> {pathTo}");
            var res = PathHelpers.ExecutableRelativeFile(pathFrom);
            if (!File.Exists(res))
                return;

            archive[pathTo] = new OnDemandDiskFile(res);
        }
    }

    // -- Information Input --

    public void SetAczInfo(string clientBinFolder, string[] clientAssemblyNames)
    {
        _aczLock.Wait();
        try
        {
            if (_aczPrepared != null)
                throw new InvalidOperationException("ACZ already prepared");

            _aczInfo = (clientBinFolder, clientAssemblyNames);
        }
        finally
        {
            _aczLock.Release();
        }
    }

    // -- This Thing --

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

}

