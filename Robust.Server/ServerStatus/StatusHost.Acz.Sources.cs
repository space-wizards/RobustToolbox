using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Robust.Packaging;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Server.ServerStatus;

// Contains source logic for ACZ (Automatic Client Zip)
// This entails the following:
// * Automatic generation of client zip on development servers. ("Magic ACZ")
// * Loading of pre-built client zip on release servers. ("Hybrid ACZ")

internal sealed partial class StatusHost
{
    private (string binFolder, string[] assemblies)? _aczInfo;
    private IMagicAczProvider? _aczProvider;

    // -- Dictionary<string, OnDemandFile> methods --

    private async Task<Dictionary<string, OnDemandFile>?> SourceAczDictionary()
    {
        return SourceAczDictionaryViaFile() ?? await SourceAczDictionaryViaMagic();
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

    private async Task<Dictionary<string, OnDemandFile>?> SourceAczDictionaryViaMagic()
    {
        var archive = new Dictionary<string, OnDemandFile>();
        var writer = new PackageWriterAcz(archive);
        var provider = _aczProvider;
        if (provider == null)
        {
            // Default provider
            var (binFolderPath, assemblyNames) =
                _aczInfo ?? ("Content.Client", new[] { "Content.Client", "Content.Shared" });

            var info = new DefaultMagicAczInfo(binFolderPath, assemblyNames);
            provider = new DefaultMagicAczProvider(info, _deps);
        }

        await provider.Package(writer, default);

        return archive;
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

    public void SetMagicAczProvider(IMagicAczProvider provider)
    {
        _aczProvider = provider;
    }

    // -- This Thing --

    /// <summary>
    /// An attempt to mitigate the amount of RAM usage caused by ACZ-related operations.
    /// The idea is that <c>Dictionary&lt;string, OnDemandFile&gt;</c> should become the standard interchange.
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

    internal sealed class OnDemandFileBlob : OnDemandFile
    {
        private readonly byte[] _blob;

        public OnDemandFileBlob(byte[] blob) : base(blob.Length)
        {
            _blob = blob;
        }

        public override void ReadExact(Span<byte> data)
        {
            _blob.AsSpan().CopyTo(data);
        }
    }

    private sealed class PackageWriterAcz : IPackageWriter
    {
        private readonly Dictionary<string, OnDemandFile> _files;

        public PackageWriterAcz(Dictionary<string, OnDemandFile> files)
        {
            _files = files;
        }

        public void WriteResource(string path, Stream stream)
        {
            var file = new OnDemandFileBlob(stream.CopyToArray());
            lock (_files)
            {
                _files[path] = file;
            }
        }

        public void WriteResourceFromDisk(string path, string diskPath)
        {
            var file = new OnDemandDiskFile(diskPath);
            lock (_files)
            {
                _files[path] = file;
            }
        }
    }
}

