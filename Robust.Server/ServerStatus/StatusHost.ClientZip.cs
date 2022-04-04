using System;
using System.Buffers.Binary;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Robust.Shared.Utility.Collections;

namespace Robust.Server.ServerStatus
{
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

            await context.RespondAsync(result.ManifestData, HttpStatusCode.OK);
            return true;
        }

        private async Task<bool> HandleAczManifestDownload(IStatusHandlerContext context)
        {
            if (context.Url.AbsolutePath != "/download")
                return false;

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

            var manifestLength = aczInfo.ManifestZipEntries.Length;
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

            var zstd = context.RequestHeaders.TryGetValue("Accept-Encoding", out var ac) && ac[0].Contains("zstd");

            if (zstd)
                context.ResponseHeaders["Content-Encoding"] = "zstd";

            var outStream = await context.RespondStreamAsync();

            if (zstd)
                outStream = new ZStdCompressStream(outStream);

            await using (outStream)
            {
                using var zip = OpenZip(aczInfo.ZipData);

                offset = 0;
                while (offset < buf.Length)
                {
                    var index = BinaryPrimitives.ReadInt32LittleEndian(buf.Slice(offset, 4).Span);

                    var entryIdx = aczInfo.ManifestZipEntries[index];

                    var entry = zip.Entries[entryIdx];

                    var length = (int)entry.Length;
                    if (!BitConverter.IsLittleEndian)
                        length = BinaryPrimitives.ReverseEndianness(length);

                    //await outStream.WriteAsync(MemoryMarshal
                    //    .Cast<Sha256Hash, byte>(MemoryMarshal.CreateReadOnlySpan(ref hash, 1)).ToArray());
                    await outStream.WriteAsync(MemoryMarshal
                        .Cast<int, byte>(MemoryMarshal.CreateReadOnlySpan(ref length, 1)).ToArray());

                    await using var entryStream = entry.Open();

                    await entryStream.CopyToAsync(outStream);

                    offset += 4;
                }
            }

            return true;
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
                        _httpSawmill.Error("StatusHost PrepareACZ failed (server will not be usable from launcher!)");
                        return null;
                    }

                    _aczPrepared = maybeData;
                    return maybeData;
                }
                catch (Exception e)
                {
                    _httpSawmill.Error(
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
            _httpSawmill.Info("Preparing ACZ...");
            // All of these should Info on success and Error on null-return failure
            var data = PrepareACZViaFile() ?? PrepareACZViaMagic();
            if (data == null)
                return null;

            _httpSawmill.Debug("Making ACZ manifest...");
            var dataHash = Convert.ToHexString(SHA256.HashData(data));
            using var zip = OpenZip(data);
            var (manifestData, zipOrdinals) = CalcManifestData(zip);
            var manifestHash = Convert.ToHexString(SHA256.HashData(manifestData));

            _aczSawmill.Debug("ACZ Manifest hash: {ManifestHash}", manifestHash);

            return new AutomaticClientZipInfo(data, dataHash, manifestData, manifestHash, zipOrdinals);
        }

        private (byte[] manifestContent, int[] zipOrdinals) CalcManifestData(ZipArchive zip)
        {
            // TODO: hash incrementally without buffering in-memory
            var manifestStream = new MemoryStream();
            var manifestWriter = new StreamWriter(manifestStream, EncodingHelpers.UTF8);
            manifestWriter.Write("Robust Content Manifest 1\n");

            var hasher = SHA256.Create();

            var zipOrdinals = new ValueList<int>();

            foreach (var (entry, i) in zip.Entries.Select((e, i) => (e, i))
                         .OrderBy(e => e.e.FullName, StringComparer.Ordinal))
            {
                // Ignore directory entries.
                if (entry.Name == "")
                    continue;

                byte[] entryHash;
                using (var stream = entry.Open())
                {
                    entryHash = hasher.ComputeHash(stream);
                }

                manifestWriter.Write($"{Convert.ToHexString(entryHash)} {entry.FullName}\n");

                zipOrdinals.Add(i);
            }

            manifestWriter.Flush();

            return (manifestStream.ToArray(), zipOrdinals.ToArray());
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
            _httpSawmill.Info($"StatusHost found client zip: {path}");
            return File.ReadAllBytes(path);
        }

        private byte[]? PrepareACZViaMagic()
        {
            var paths = new Dictionary<string, byte[]>();

            bool AttemptPullFromDisk(string pathTo, string pathFrom)
            {
                // _httpSawmill.Debug($"StatusHost PrepareACZMagic: {pathFrom} -> {pathTo}");
                var res = PathHelpers.ExecutableRelativeFile(pathFrom);
                if (!File.Exists(res)) return false;
                paths[pathTo] = File.ReadAllBytes(res);
                return true;
            }

            var (binFolderPath, assemblyNames) =
                _aczInfo ?? ("Content.Client", new[] { "Content.Client", "Content.Shared" });

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

            var outStream = new MemoryStream();
            var archive = new ZipArchive(outStream, ZipArchiveMode.Create);
            foreach (var kvp in paths)
            {
                var entry = archive.CreateEntry(kvp.Key);
                using (var entryStream = entry.Open())
                {
                    entryStream.Write(kvp.Value);
                }
            }

            archive.Dispose();
            _httpSawmill.Info($"StatusHost synthesized client zip!");
            return outStream.ToArray();
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
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="ZipData">Byte array containing the raw zip file data.</param>
    /// <param name="ZipHash">Hex SHA256 hash of <see cref="ZipData"/>.</param>
    /// <param name="ManifestData">Data for the content manifest</param>
    /// <param name="ManifestHash">Hex SHA256 hash of <see cref="ManifestData"/>.</param>
    /// <param name="ManifestZipEntries">Manifest -> zip entry map.</param>
    internal sealed record AutomaticClientZipInfo(
        byte[] ZipData,
        string ZipHash,
        byte[] ManifestData,
        string ManifestHash,
        int[] ManifestZipEntries);
}
