using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Robust.Packaging.AssetProcessing;
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

    private async Task<List<OnDemandFile>?> SourceAczDictionary()
    {
        return SourceAczDictionaryViaFile() ?? await SourceAczDictionaryViaMagic();
    }

    private List<OnDemandFile>? SourceAczDictionaryViaFile()
    {
        var path = PathHelpers.ExecutableRelativeFile("Content.Client.zip");
        if (!File.Exists(path)) return null;
        _aczSawmill.Info($"StatusHost found client zip: {path}");
        // Note: We don't want to explicitly close this, as the OnDemandFiles will hold references to this.
        // Let it be cleaned up by GC eventually.
        FileStream fs = File.OpenRead(path);
        return SourceAczDictionaryViaZipStream(fs);
    }

    private List<OnDemandFile> SourceAczDictionaryViaZipStream(Stream stream)
    {
        var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var archive = new List<OnDemandFile>();
        foreach (var entry in zip.Entries)
        {
            // Ignore directory entries.
            if (entry.Name == "")
                continue;

            archive.Add(new OnDemandZipArchiveEntryFile(entry));
        }
        return archive;
    }

    private async Task<List<OnDemandFile>?> SourceAczDictionaryViaMagic()
    {
        var archive = new List<OnDemandFile>();
        var writer = new AssetPassAczWriter(archive);
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

        await writer.FinishedTask;

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
        public readonly string Path;

        /// <summary>
        /// Length of the target file. Assumed to be cached.
        /// </summary>
        public readonly long Length;

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

        public OnDemandFile(string path, long len)
        {
            Path = path;
            Length = len;
        }

        public abstract void ReadExact(Span<byte> data);
    }

    internal sealed class OnDemandDiskFile : OnDemandFile
    {
        private readonly string _diskPath;

        public OnDemandDiskFile(string path, string fileName) : base(path, new FileInfo(fileName).Length)
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

        public OnDemandZipArchiveEntryFile(ZipArchiveEntry src) : base(src.FullName, src.Length)
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

        public OnDemandFileBlob(string path, byte[] blob) : base(path, blob.Length)
        {
            _blob = blob;
        }

        public override void ReadExact(Span<byte> data)
        {
            _blob.AsSpan().CopyTo(data);
        }
    }

    private sealed class AssetPassAczWriter : AssetPass
    {
        private readonly List<OnDemandFile> _files;

        public AssetPassAczWriter(List<OnDemandFile> files)
        {
            _files = files;
        }

        public override AssetFileAcceptResult AcceptFile(AssetFile file)
        {
            lock (_files)
            {
                switch (file)
                {
                    case AssetFileDisk assetFileDisk:
                        _files.Add(new OnDemandDiskFile(assetFileDisk.Path, assetFileDisk.DiskPath));
                        break;

                    case AssetFileMemory assetFileMemory:
                        _files.Add(new OnDemandFileBlob(assetFileMemory.Path, assetFileMemory.Memory));
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }

            return AssetFileAcceptResult.Consumed;
        }
    }

    private sealed class OnDemandFilePathComparer : IComparer<OnDemandFile>
    {
        public static readonly OnDemandFilePathComparer Instance = new();

        public int Compare(OnDemandFile? x, OnDemandFile? y)
        {
            return string.Compare(x!.Path, y!.Path, StringComparison.Ordinal);
        }
    }
}

