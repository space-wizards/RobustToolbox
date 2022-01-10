using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using Robust.Shared;
using Robust.Shared.ContentPack;

namespace Robust.Server.ServerStatus
{

    internal sealed partial class StatusHost
    {
        // Lock used while working on the ACZ.
        private readonly SemaphoreSlim _aczLock = new(1, 1);
        // If an attempt has been made to prepare the ACZ.
        private bool _aczPrepareAttempted = false;
        // Automatic Client Zip
        private AutomaticClientZipInfo? _aczPrepared;

        private (string binFolder, string[] assemblies)? _aczInfo;

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
                await context.RespondAsync("Automatic Client Zip was not preparable.", HttpStatusCode.InternalServerError);
                return true;
            }

            await context.RespondAsync(result.Value.Data, HttpStatusCode.OK, "application/zip");
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
                if (_aczPrepareAttempted) return _aczPrepared;
                _aczPrepareAttempted = true;
                // ACZ hasn't been prepared, prepare it
                byte[] data;
                try
                {
                    // Run actual ACZ generation via Task.Run because it's synchronous
                    var maybeData = await Task.Run(PrepareACZInnards);
                    if (maybeData == null)
                    {
                        _httpSawmill.Error("StatusHost PrepareACZ failed (server will not be usable from launcher!)");
                        return null;
                    }
                    data = maybeData;
                }
                catch (Exception e)
                {
                    _httpSawmill.Error($"Exception in StatusHost PrepareACZ (server will not be usable from launcher!): {e}");
                    return null;
                }
                _aczPrepared = new AutomaticClientZipInfo(data);
                return _aczPrepared;
            }
            finally
            {
                _aczLock.Release();
            }
        }

        // -- All methods from this point forward do not access the ACZ global state --

        private byte[]? PrepareACZInnards()
        {
            // All of these should Info on success and Error on null-return failure
            return PrepareACZViaFile() ?? PrepareACZViaMagic();
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

    internal struct AutomaticClientZipInfo
    {
        public readonly byte[] Data;
        public readonly string Hash;

        public AutomaticClientZipInfo(byte[] data)
        {
            Data = data;
            using var sha = SHA256.Create();
            Hash = Convert.ToHexString(sha.ComputeHash(data));
        }
    }
}
