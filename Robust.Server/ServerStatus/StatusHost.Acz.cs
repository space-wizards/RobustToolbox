using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
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
using Robust.Shared.Utility.Collections;
using SharpZstd.Interop;
using SpaceWizards.Sodium;

namespace Robust.Server.ServerStatus
{
    // Contains primary logic for ACZ (Automatic Client Zip)
    // This entails the following:
    // * Automatic generation of client zip on development servers.
    // * Loading of pre-built client zip on release servers. ("Hybrid ACZ")
    // * Distribution of the above two via status host, to facilitate easier server setup.
    // * Manifest-based download system from the above.

    internal sealed partial class StatusHost
    {
        // Enough buffer for a request of 100k files.
        private const int MaxAczDownloadRequestSize = 4 * 100_000;

        // Lock used while working on the ACZ.
        private readonly SemaphoreSlim _aczLock = new(1, 1);

        // If an attempt has been made to prepare the ACZ.
        private bool _aczPrepareAttempted = false;

        // Automatic Client Zip
        private AutomaticClientZipInfo? _aczPrepared;

        private (string binFolder, string[] assemblies)? _aczInfo;

        private void AddAczHandlers()
        {
            AddHandler(HandleAutomaticClientZip);
            AddHandler(HandleAczManifest);
            AddHandler(HandleAczManifestDownload);
        }

        private void InitAcz()
        {
            _configurationManager.OnValueChanged(CVars.AczStreamCompress, _ => InvalidateAcz());
            _configurationManager.OnValueChanged(CVars.AczStreamCompressLevel, _ => InvalidateAcz());
            _configurationManager.OnValueChanged(CVars.AczBlobCompress, _ => InvalidateAcz());
            _configurationManager.OnValueChanged(CVars.AczBlobCompressLevel, _ => InvalidateAcz());
            _configurationManager.OnValueChanged(CVars.AczBlobCompressSaveThreshold, _ => InvalidateAcz());
            _configurationManager.OnValueChanged(CVars.AczManifestCompress, _ => InvalidateAcz());
            _configurationManager.OnValueChanged(CVars.AczManifestCompressLevel, _ => InvalidateAcz());
        }

        private void InvalidateAcz()
        {
            using var _ = _aczLock.WaitGuard();

            if (_aczPrepared == null)
                return;

            _aczSawmill.Info("ACZ CVars changed, invalidating ACZ data.");

            _aczPrepared = null;
            _aczPrepareAttempted = false;
        }

        private async Task<bool> HandleAutomaticClientZip(IStatusHandlerContext context)
        {
            if (!context.IsGetLike || context.Url!.AbsolutePath != "/client.zip")
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_configurationManager.GetCVar(CVars.BuildDownloadUrl)))
            {
                await context.RespondAsync("This server has a build download URL.", HttpStatusCode.NotFound);
                return true;
            }

            var result = await PrepareACZ();
            if (result == null)
            {
                await context.RespondAsync("Automatic Client Zip was not preparable.",
                    HttpStatusCode.InternalServerError);
                return true;
            }

            await context.RespondAsync(result.ZipData, HttpStatusCode.OK, "application/zip");
            return true;
        }

        private async Task<bool> HandleAczManifest(IStatusHandlerContext context)
        {
            if (!context.IsGetLike || context.Url!.AbsolutePath != "/manifest.txt")
                return false;

            if (!string.IsNullOrEmpty(_configurationManager.GetCVar(CVars.BuildManifestUrl)))
            {
                await context.RespondAsync("This server has a build manifest URL.", HttpStatusCode.NotFound);
                return true;
            }

            var result = await PrepareACZ();
            if (result == null)
            {
                await context.RespondAsync("Automatic Client Zip was not preparable.",
                    HttpStatusCode.InternalServerError);
                return true;
            }

            if (RequestWantsZStd(context) && result.ManifestCompressed)
            {
                context.ResponseHeaders.Add("Content-Encoding", "zstd");

                await context.RespondAsync(result.ManifestData, HttpStatusCode.OK);
            }
            else
            {
                if (result.ManifestCompressed)
                {
                    // Manifest is compressed in-memory but client didn't want it compressed.
                    // Have to decompress ourselves.

                    var ms = new MemoryStream(result.ManifestData);

                    await using var stream = await context.RespondStreamAsync();
                    await using var decompressStream = new ZStdDecompressStream(ms);

                    await decompressStream.CopyToAsync(stream);
                }
                else
                {
                    await context.RespondAsync(result.ManifestData, HttpStatusCode.OK);
                }
            }

            return true;
        }

        private async Task<bool> HandleAczManifestDownload(IStatusHandlerContext context)
        {
            if (context.Url.AbsolutePath != "/download")
                return false;

            if (context.RequestHeaders.ContainsKey("Content-Type")
                && context.RequestHeaders["Content-Type"] != "application/octet-stream")
            {
                await context.RespondAsync(
                    "Must specify application/octet-stream Content-Type",
                    HttpStatusCode.BadRequest);
            }

            if (!string.IsNullOrEmpty(_configurationManager.GetCVar(CVars.BuildManifestUrl)))
            {
                await context.RespondAsync("This server has a build manifest URL.", HttpStatusCode.NotFound);
                return true;
            }

            // HTTP OPTIONS
            if (context.RequestMethod == HttpMethod.Options)
            {
                context.ResponseHeaders["X-Robust-Download-Min-Protocol"] = "1";
                context.ResponseHeaders["X-Robust-Download-Max-Protocol"] = "1";
                await context.RespondNoContentAsync();
                return true;
            }

            if (context.RequestMethod != HttpMethod.Post)
                return false;

            var aczInfo = await PrepareACZ();
            if (aczInfo == null)
            {
                await context.RespondAsync("Automatic Client Zip was not preparable.",
                    HttpStatusCode.InternalServerError);
                return true;
            }

            // HTTP POST: main handling system.

            // Verify version request header.
            // Right now only one version ("1") exists, so...

            // Request body not yet read, don't allow keepalive.
            context.KeepAlive = false;
            if (!context.RequestHeaders.TryGetValue("X-Robust-Download-Protocol", out var versionHeader)
                || versionHeader.Count != 1
                || !Parse.TryInt32(versionHeader[0], out var version))
            {
                await context.RespondAsync("Expected single X-Robust-Download-Protocol header",
                    HttpStatusCode.BadRequest);
                return true;
            }

            if (version != 1)
            {
                await context.RespondAsync("Unsupported download protocol version", HttpStatusCode.NotImplemented);
                return true;
            }

            // TODO: Don't overallocate.
            // Important: don't allow memory stream to buffer indefinitely, limit request size.
            var buffer = new MemoryStream(
                new byte[MaxAczDownloadRequestSize],
                0, MaxAczDownloadRequestSize,
                writable: true,
                publiclyVisible: true);

            try
            {
                await context.RequestBody.CopyToAsync(buffer);
            }
            catch (NotSupportedException)
            {
                // Thrown by memory stream if full.
                await context.RespondAsync("Request too large", HttpStatusCode.RequestEntityTooLarge);
                return true;
            }

            // Request body read, allow keepalive again.
            context.KeepAlive = true;

            // Request body read. Validate it.
            // Do not allow out-of-bounds files or duplicate requests.

            var buf = buffer.GetBuffer().AsMemory(0, (int)buffer.Position);

            var manifestLength = aczInfo.ManifestEntries.Length;
            var bits = new BitArray(manifestLength);
            var offset = 0;
            while (offset < buf.Length)
            {
                var index = BinaryPrimitives.ReadInt32LittleEndian(buf.Slice(offset, 4).Span);

                if (index < 0 || index >= manifestLength)
                {
                    await context.RespondAsync("Out of bounds manifest index", HttpStatusCode.BadRequest);
                    return true;
                }

                if (bits[index])
                {
                    await context.RespondAsync("Cannot request file twice", HttpStatusCode.BadRequest);
                    return true;
                }

                bits[index] = true;

                offset += 4;
            }

            // There is a theoretical tiny race condition here where the main thread may change these parameters
            // between us acquiring the ACZ info above and reading them here.
            // The worst that could happen here is that the stream is either double-compressed or not compressed at all,
            // So I am not too worried and am just gonna leave it as-is.

            var cVarStreamCompression = _configurationManager.GetCVar(CVars.AczStreamCompress);
            var cVarStreamCompressionLevel = _configurationManager.GetCVar(CVars.AczStreamCompressLevel);

            // Only do zstd stream compression if the client asks for it and we have it enabled.
            var doStreamCompression = RequestWantsZStd(context)
                                      && cVarStreamCompression;

            if (doStreamCompression)
                context.ResponseHeaders["Content-Encoding"] = "zstd";

            var outStream = await context.RespondStreamAsync();

            if (doStreamCompression)
            {
                var zStdCompressStream = new ZStdCompressStream(outStream);
                zStdCompressStream.Context.SetParameter(
                    ZSTD_cParameter.ZSTD_c_compressionLevel,
                    cVarStreamCompressionLevel);

                outStream = zStdCompressStream;
            }

            var preCompressed = aczInfo.PreCompressed;

            var fileHeaderSize = 4;
            if (preCompressed)
                fileHeaderSize += 4;

            var fileHeader = new byte[fileHeaderSize];

            await using (outStream)
            {
                var streamHeader = new byte[4];
                DownloadStreamHeaderFlags streamHeaderFlags = 0;
                if (preCompressed)
                    streamHeaderFlags |= DownloadStreamHeaderFlags.PreCompressed;

                BinaryPrimitives.WriteInt32LittleEndian(streamHeader, (int)streamHeaderFlags);

                await outStream.WriteAsync(streamHeader);

                offset = 0;
                while (offset < buf.Length)
                {
                    var index = BinaryPrimitives.ReadInt32LittleEndian(buf.Slice(offset, 4).Span);

                    var (blobLength, dataOffset, dataLength) = aczInfo.ManifestEntries[index];

                    // _aczSawmill.Debug($"{index:D5}: {blobLength:D8} {dataOffset:D8} {dataLength:D8}");

                    BinaryPrimitives.WriteInt32LittleEndian(fileHeader, blobLength);

                    if (preCompressed)
                        BinaryPrimitives.WriteInt32LittleEndian(fileHeader.AsSpan(4, 4), dataLength);

                    var writeLength = dataLength == 0 ? blobLength : dataLength;

                    await outStream.WriteAsync(fileHeader);

                    await outStream.WriteAsync(aczInfo.ManifestBlobData.AsMemory(dataOffset, writeLength));

                    offset += 4;
                }
            }

            return true;
        }

        private static bool RequestWantsZStd(IStatusHandlerContext context)
        {
            // Yeah this isn't a good parser for Accept-Encoding but who cares.
            return context.RequestHeaders.TryGetValue("Accept-Encoding", out var ac) && ac[0].Contains("zstd");
        }

        // Only call this if the download URL is not available!
        private async Task<AutomaticClientZipInfo?> PrepareACZ()
        {
            // Take the ACZ lock asynchronously
            await _aczLock.WaitAsync();
            try
            {
                // Setting this now ensures that it won't fail repeatedly on exceptions/etc.
                if (_aczPrepareAttempted)
                    return _aczPrepared;

                _aczPrepareAttempted = true;
                // ACZ hasn't been prepared, prepare it
                try
                {
                    // Run actual ACZ generation via Task.Run because it's synchronous
                    var maybeData = await Task.Run(PrepareACZInnards);
                    if (maybeData == null)
                    {
                        _aczSawmill.Error("StatusHost PrepareACZ failed (server will not be usable from launcher!)");
                        return null;
                    }

                    _aczPrepared = maybeData;
                    return maybeData;
                }
                catch (Exception e)
                {
                    _aczSawmill.Error(
                        $"Exception in StatusHost PrepareACZ (server will not be usable from launcher!): {e}");
                    return null;
                }
            }
            finally
            {
                _aczLock.Release();
            }
        }

        // -- All methods from this point forward do not access the ACZ global state --

        private AutomaticClientZipInfo? PrepareACZInnards()
        {
            _aczSawmill.Info("Preparing ACZ...");
            // All of these should Info on success and Error on null-return failure
            var zipData = PrepareACZViaFile() ?? PrepareACZViaMagic();
            if (zipData == null)
                return null;

            var streamCompression = _configurationManager.GetCVar(CVars.AczStreamCompress);
            var blobCompress = _configurationManager.GetCVar(CVars.AczBlobCompress);
            var blobCompressLevel = _configurationManager.GetCVar(CVars.AczBlobCompressLevel);
            var blobCompressSaveThresh = _configurationManager.GetCVar(CVars.AczBlobCompressSaveThreshold);
            var manifestCompress = _configurationManager.GetCVar(CVars.AczManifestCompress);
            var manifestCompressLevel = _configurationManager.GetCVar(CVars.AczManifestCompressLevel);

            // Stream compression disables individual compression.
            blobCompress &= !streamCompression;

            _aczSawmill.Debug("Making ACZ manifest...");
            var dataHash = Convert.ToHexString(SHA256.HashData(zipData));

            using var zip = OpenZip(zipData);
            var (manifestData, manifestEntries, manifestBlobData) = CalcManifestData(
                zip,
                blobCompress,
                blobCompressLevel,
                blobCompressSaveThresh);

            var manifestHash = CryptoGenericHashBlake2B.Hash(32, manifestData, ReadOnlySpan<byte>.Empty);
            var manifestHashString = Convert.ToHexString(manifestHash);

            _aczSawmill.Debug("ACZ Manifest hash: {ManifestHash}", manifestHashString);

            if (manifestCompress)
            {
                _aczSawmill.Debug("Compressing ACZ manifest at level {ManifestCompressLevel}", manifestCompressLevel);

                var beforeSize = manifestData.Length;
                var compressBuffer = (int) Zstd.ZSTD_COMPRESSBOUND((nuint) manifestData.Length);
                var compressed = ArrayPool<byte>.Shared.Rent(compressBuffer);

                var size = ZStd.Compress(compressed, manifestData, manifestCompressLevel);

                manifestData = compressed[..size];

                ArrayPool<byte>.Shared.Return(compressed);

                _aczSawmill.Debug(
                    "ACZ manifest compression: {ManifestSize} -> {ManifestSizeCompressed} ({ManifestSizeRatio} ratio)",
                    beforeSize, manifestData.Length, manifestData.Length / (float) beforeSize);
            }

            return new AutomaticClientZipInfo(
                zipData,
                dataHash,
                manifestData,
                manifestCompress,
                manifestHashString,
                manifestBlobData,
                manifestEntries,
                blobCompress);
        }

        private static (byte[] manifestContent, AczManifestEntry[] manifestEntries, byte[] blobData)
            CalcManifestData(
                ZipArchive zip,
                bool blobCompress,
                int blobCompressLevel,
                int blobCompressSaveThresh)
        {
            var blobData = new MemoryStream();
            ZStdCompressStream? compressStream = null;
            if (blobCompress)
            {
                var zStdCompressStream = new ZStdCompressStream(blobData);
                zStdCompressStream.Context.SetParameter(
                    ZSTD_cParameter.ZSTD_c_compressionLevel,
                    blobCompressLevel);

                compressStream = zStdCompressStream;
            }

            try
            {
                var decompressBuffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);
                Span<byte> entryHash = stackalloc byte[256 / 8];

                var manifestStream = new MemoryStream();
                using var manifestWriter = new StreamWriter(manifestStream, EncodingHelpers.UTF8);
                manifestWriter.Write("Robust Content Manifest 1\n");

                var manifestEntries = new ValueList<AczManifestEntry>();

                foreach (var entry in zip.Entries.OrderBy(e => e.FullName, StringComparer.Ordinal))
                {
                    // Ignore directory entries.
                    if (entry.Name == "")
                        continue;

                    var length = (int)entry.Length;
                    var startPos = (int)blobData.Position;

                    BufferHelpers.EnsurePooledBuffer(ref decompressBuffer, ArrayPool<byte>.Shared, length);
                    var data = decompressBuffer.AsSpan(0, length);

                    using (var stream = entry.Open())
                    {
                        stream.ReadExact(data);
                    }

                    // Calculate hash.
                    CryptoGenericHashBlake2B.Hash(entryHash, data, ReadOnlySpan<byte>.Empty);

                    // Set to 0 to indicate not compressed.
                    int dataLength;

                    // Try compression if enabled.
                    if (blobCompress)
                    {
                        // Actually compress.
                        compressStream!.Write(data);
                        compressStream.FlushEnd();

                        // See if compression was worth it.
                        var endPos = (int)blobData.Position;
                        var compressedSize = endPos - startPos;
                        if (compressedSize + blobCompressSaveThresh < length)
                        {
                            dataLength = compressedSize;
                        }
                        else
                        {
                            // Compression not worth it, just send an uncompressed blob instead.
                            blobData.Position = startPos;
                            blobData.Write(data);
                            dataLength = 0;
                        }
                    }
                    else
                    {
                        // No compression, just write.
                        blobData.Write(data);
                        dataLength = 0;
                    }

                    manifestWriter.Write($"{Convert.ToHexString(entryHash)} {entry.FullName}\n");

                    manifestEntries.Add(new AczManifestEntry(length, startPos, dataLength));
                }

                manifestWriter.Flush();

                ArrayPool<byte>.Shared.Return(decompressBuffer);

                return (manifestStream.ToArray(), manifestEntries.ToArray(), blobData.ToArray());
            }
            finally
            {
                compressStream?.Dispose();
            }
        }

        private static ZipArchive OpenZip(byte[] data)
        {
            var ms = new MemoryStream(data, false);
            return new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);
        }

        private byte[]? PrepareACZViaFile()
        {
            var path = PathHelpers.ExecutableRelativeFile("Content.Client.zip");
            if (!File.Exists(path)) return null;
            _aczSawmill.Info($"StatusHost found client zip: {path}");
            return File.ReadAllBytes(path);
        }

        private byte[]? PrepareACZViaMagic()
        {
            var sw = Stopwatch.StartNew();

            var (binFolderPath, assemblyNames) =
                _aczInfo ?? ("Content.Client", new[] { "Content.Client", "Content.Shared" });

            var outStream = new MemoryStream();
            var archive = new ZipArchive(outStream, ZipArchiveMode.Create);

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

            archive.Dispose();
            _aczSawmill.Info("StatusHost synthesized client zip in {Elapsed} ms!", sw.ElapsedMilliseconds);
            return outStream.ToArray();

            void AttemptPullFromDisk(string pathTo, string pathFrom)
            {
                // _aczSawmill.Debug($"StatusHost PrepareACZMagic: {pathFrom} -> {pathTo}");
                var res = PathHelpers.ExecutableRelativeFile(pathFrom);
                if (!File.Exists(res))
                    return;

                var entry = archive.CreateEntry(pathTo);

                using var file = File.OpenRead(res);
                using var entryStream = entry.Open();

                file.CopyTo(entryStream);
            }
        }

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

        [Flags]
        private enum DownloadStreamHeaderFlags
        {
            None = 0,

            /// <summary>
            /// If this flag is set on the download stream, individual files have been pre-compressed by the server.
            /// This means each file has a compression header, and the launcher should not attempt to compress files itself.
            /// </summary>
            PreCompressed = 1 << 0
        }

        /// <param name="ZipData">Byte array containing the raw zip file data.</param>
        /// <param name="ZipHash">Hex SHA256 hash of <see cref="ZipData"/>.</param>
        /// <param name="ManifestData">Data for the content manifest</param>
        /// <param name="ManifestHash">Hex BLAKE2B 256-bit hash of <see cref="ManifestData"/>.</param>
        /// <param name="ManifestEntries">Manifest -> zip entry map.</param>
        internal sealed record AutomaticClientZipInfo(
            byte[] ZipData,
            string ZipHash,
            byte[] ManifestData,
            bool ManifestCompressed,
            string ManifestHash,
            byte[] ManifestBlobData,
            AczManifestEntry[] ManifestEntries,
            bool PreCompressed);

        /// <param name="BlobLength">Length of the uncompressed blob.</param>
        /// <param name="DataOffset">Offset into <see cref="AutomaticClientZipInfo.ManifestBlobData"/> that this blob's (possibly compressed) data starts at.</param>
        /// <param name="DataLength">
        /// Length in <see cref="AutomaticClientZipInfo.ManifestBlobData"/> for this blob's (possibly compressed) data.
        /// If this is zero, it means the file is not stored uncompressed and you should use <see cref="BlobLength"/>.
        /// </param>
        internal record struct AczManifestEntry(int BlobLength, int DataOffset, int DataLength);
    }
}
