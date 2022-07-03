using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;
using Robust.Packaging.AssetProcessing.Passes;
using Robust.Shared;
using Robust.Shared.Collections;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using SpaceWizards.Sodium;

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

    private async Task<AczManifestInfo?> PrepareAczInner()
    {
        var streamCompression = _cfg.GetCVar(CVars.AczStreamCompress);
        var blobCompress = _cfg.GetCVar(CVars.AczBlobCompress);
        var blobCompressLevel = _cfg.GetCVar(CVars.AczBlobCompressLevel);
        var blobCompressSaveThresh = _cfg.GetCVar(CVars.AczBlobCompressSaveThreshold);
        var manifestCompress = _cfg.GetCVar(CVars.AczManifestCompress);
        var manifestCompressLevel = _cfg.GetCVar(CVars.AczManifestCompressLevel);

        // Stream compression disables individual compression.
        blobCompress &= !streamCompression;

        var manifestResult = await CalcManifestData(
            blobCompress,
            blobCompressLevel,
            blobCompressSaveThresh);

        if (manifestResult == null)
            return null;

        var (manifestData, manifestEntries, manifestBlobData) = manifestResult!.Value;

        var manifestHash = CryptoGenericHashBlake2B.Hash(32, manifestData, ReadOnlySpan<byte>.Empty);
        var manifestHashString = Convert.ToHexString(manifestHash);

        _aczSawmill.Debug("ACZ Manifest hash: {ManifestHash}", manifestHashString);

        if (manifestCompress)
        {
            _aczSawmill.Debug("Compressing ACZ manifest at level {ManifestCompressLevel}", manifestCompressLevel);

            var beforeSize = manifestData.Length;
            var compressBuffer = ZStd.CompressBound(manifestData.Length);
            var compressed = ArrayPool<byte>.Shared.Rent(compressBuffer);

            var size = ZStd.Compress(compressed, manifestData, manifestCompressLevel);

            manifestData = compressed[..size];

            ArrayPool<byte>.Shared.Return(compressed);

            _aczSawmill.Debug(
                "ACZ manifest compression: {ManifestSize} -> {ManifestSizeCompressed} ({ManifestSizeRatio} ratio)",
                beforeSize, manifestData.Length, manifestData.Length / (float) beforeSize);
        }

        return new AczManifestInfo(
            manifestData,
            manifestCompress,
            manifestHashString,
            manifestBlobData,
            manifestEntries,
            blobCompress);
    }

    private async Task<(byte[] manifestData, AczManifestEntry[] entries, byte[] blobData)?> CalcManifestData(
        bool blobCompress,
        int blobCompressLevel,
        int blobCompressSaveThresh)
    {
        var logger = new PackageLoggerSawmill(_aczPackagingSawmill);
        using var writerPass = new AssetPassAczWriter(blobCompress, blobCompressLevel, blobCompressSaveThresh);

        var result = await SourceAczDictionaryViaFile(writerPass, logger) ||
                     await SourceAczViaMagic(writerPass, logger);

        if (!result)
            return null;

        await writerPass.FinishedTask;

        return (writerPass.ManifestContent!, writerPass.ManifestEntries!, writerPass.BlobData!);
    }

    private Task<bool> SourceAczDictionaryViaFile(AssetPass pass, IPackageLogger logger)
    {
        var path = PathHelpers.ExecutableRelativeFile("Content.Client.zip");
        if (!File.Exists(path))
            return Task.FromResult(false);

        _aczSawmill.Info($"StatusHost found client zip: {path}");
        using var zip = new ZipArchive(File.OpenRead(path), ZipArchiveMode.Read, leaveOpen: false);
        SourceAczDictionaryViaZipStream(zip, pass, logger);
        return Task.FromResult(true);
    }

    private static void SourceAczDictionaryViaZipStream(ZipArchive zip, AssetPass pass, IPackageLogger logger)
    {
        var inputPass = new AssetPassPipe { Parallelize = true };
        pass.AddDependency(inputPass);

        AssetGraph.CalculateGraph(new []{inputPass, pass}, logger);

        foreach (var entry in zip.Entries)
        {
            // Ignore directory entries.
            if (entry.Name == "")
                continue;

            using var stream = entry.Open();
            var file = new AssetFileMemory(entry.FullName, stream.CopyToArray());
            inputPass.InjectFile(file);
        }

        inputPass.InjectFinished();
    }

    private async Task<bool> SourceAczViaMagic(AssetPass pass, IPackageLogger logger)
    {
        var provider = _aczProvider;
        if (provider == null)
        {
            // Default provider
            var (binFolderPath, assemblyNames) =
                _aczInfo ?? ("Content.Client", new[] { "Content.Client", "Content.Shared" });

            var info = new DefaultMagicAczInfo(binFolderPath, assemblyNames);
            provider = new DefaultMagicAczProvider(info, _deps);
        }

        await provider.Package(pass, logger, default);
        return true;
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

    private sealed class AssetPassAczWriter : AssetPass, IDisposable
    {
        private readonly object _lock = new();

        private readonly SequenceMemoryStream _stream = new();
        private readonly bool _blobCompress;
        private readonly int _blobCompressLevel;
        private readonly int _blobCompressSaveThresh;

        private readonly ObjectPool<ZStdCompressionContext> _compressStreamPool =
            ObjectPool.Create<ZStdCompressionContext>();

        private ValueList<InProgressAczManifestInfo> _infos;

        public byte[]? ManifestContent;
        public AczManifestEntry[]? ManifestEntries;
        public byte[]? BlobData;

        public AssetPassAczWriter(
            bool blobCompress,
            int blobCompressLevel,
            int blobCompressSaveThresh)
        {
            _blobCompress = blobCompress;
            _blobCompressLevel = blobCompressLevel;
            _blobCompressSaveThresh = blobCompressSaveThresh;
        }

        protected override AssetFileAcceptResult AcceptFile(AssetFile file)
        {
            // Logger?.Verbose(file.Path);
            var entryHash = new byte[256 / 8];

            byte[]? dataPool = null;
            Span<byte> data;

            if (file is AssetFileMemory mem)
            {
                data = mem.Memory;
            }
            else
            {
                using var fs = file.Open();

                dataPool = ArrayPool<byte>.Shared.Rent((int) fs.Length);
                data = dataPool.AsSpan(0, (int)fs.Length);

                fs.ReadToEnd(data);
            }

            CryptoGenericHashBlake2B.Hash(entryHash, data, ReadOnlySpan<byte>.Empty);

            ReadOnlySpan<byte> toWrite;

            byte[]? compressBuffer = null;
            if (_blobCompress)
            {
                var compressCtx = _compressStreamPool.Get();
                compressBuffer = ArrayPool<byte>.Shared.Rent(ZStd.CompressBound(data.Length));
                var comprLength = compressCtx.Compress(compressBuffer, data, _blobCompressLevel);

                _compressStreamPool.Return(compressCtx);

                // See if compression was worth it.
                if (comprLength + _blobCompressSaveThresh < data.Length)
                {
                    // Worth it
                    toWrite = compressBuffer.AsSpan(0, comprLength);
                }
                else
                {
                    // Compression not worth it, just send an uncompressed blob instead.
                    toWrite = data;
                }
            }
            else
            {
                toWrite = data;
            }

            lock (_lock)
            {
                var streamPos = (int)_stream.Position;
                _stream.Write(toWrite);
                var info = new AczManifestEntry(
                    data.Length,
                    streamPos,
                    toWrite.Length == data.Length ? 0 : toWrite.Length);

                _infos.Add(new InProgressAczManifestInfo(info, file.Path, entryHash));
            }

            if (compressBuffer != null)
                ArrayPool<byte>.Shared.Return(compressBuffer);

            if (dataPool != null)
                ArrayPool<byte>.Shared.Return(dataPool);

            return AssetFileAcceptResult.Consumed;
        }

        protected override void AcceptFinished()
        {
            _infos.Sort(OnDemandFilePathComparer.Instance);

            var manifestStream = new MemoryStream();
            using var manifestWriter = new StreamWriter(manifestStream, EncodingHelpers.UTF8);
            manifestWriter.Write("Robust Content Manifest 1\n");

            var manifestEntries = new AczManifestEntry[_infos.Count];

            for (var i = 0; i < _infos.Count; i++)
            {
                var info = _infos[i];
                manifestWriter.Write($"{Convert.ToHexString(info.Hash)} {info.Path}\n");

                manifestEntries[i] = info.Entry;
            }

            manifestWriter.Flush();

            ManifestContent = manifestStream.ToArray();
            ManifestEntries = manifestEntries;
            BlobData = _stream.AsSequence.ToArray();
        }

        public void Dispose()
        {
            // _compressStreamPool is actually a DisposableObjectPool, which is an internal type.
            (_compressStreamPool as IDisposable)?.Dispose();
        }
    }

    private sealed record InProgressAczManifestInfo(AczManifestEntry Entry, string Path, byte[] Hash);

    private sealed class OnDemandFilePathComparer : IComparer<InProgressAczManifestInfo>
    {
        public static readonly OnDemandFilePathComparer Instance = new();

        public int Compare(InProgressAczManifestInfo? x, InProgressAczManifestInfo? y)
        {
            return string.Compare(x!.Path, y!.Path, StringComparison.Ordinal);
        }
    }
}
