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
    // For the manifest system, see: StatusHost.ACManifest.cs
    //
    // BIG IMPORTANT NOTE:
    //  At some future point there may be no reason to bother keeping the legacy zip-based download method going.
    //  At that point it may be worth dropping the ZIP caching here.
    //  For now they're separate caches to keep the interface between the two systems clean.

    internal sealed partial class StatusHost
    {
        // Lock used while working on the ACZ.
        private readonly SemaphoreSlim _acZipLock = new(1, 1);

        // If an attempt has been made to prepare the ACZ.
        private bool _acZipPrepareAttempted = false;

        // Automatic Client Zip
        private AutomaticClientZipInfo? _acZipPrepared;

        private (string binFolder, string[] assemblies)? _aczInfo;

        private void AddACZipHandlers()
        {
            AddHandler(HandleAutomaticClientZip);
        }

        private async Task<bool> HandleAutomaticClientZip(IStatusHandlerContext context)
        {
            if (!context.IsGetLike || context.Url!.AbsolutePath != "/client.zip")
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_cfg.GetCVar(CVars.BuildDownloadUrl)))
            {
                await context.RespondAsync("This server has a build download URL.", HttpStatusCode.NotFound);
                return true;
            }

            var result = await PrepareACZip();
            if (result == null)
            {
                await context.RespondAsync("Automatic Client Zip was not preparable.",
                    HttpStatusCode.InternalServerError);
                return true;
            }

            await context.RespondAsync(result.ZipData, HttpStatusCode.OK, "application/zip");
            return true;
        }

        // Only call this if the download URL is not available!
        private async Task<AutomaticClientZipInfo?> PrepareACZip()
        {
            // Take the ACZ lock asynchronously
            await _acZipLock.WaitAsync();
            try
            {
                // Setting this now ensures that it won't fail repeatedly on exceptions/etc.
                if (_acZipPrepareAttempted)
                    return _acZipPrepared;

                _acZipPrepareAttempted = true;
                // ACZ hasn't been prepared, prepare it
                try
                {
                    // Run actual ACZ generation via Task.Run because it's synchronous
                    var maybeData = await Task.Run(PrepareACZipInnards);
                    if (maybeData == null)
                    {
                        _aczSawmill.Error("StatusHost PrepareACZip failed (server will not be usable from launcher!)");
                        return null;
                    }

                    _acZipPrepared = maybeData;
                    return maybeData;
                }
                catch (Exception e)
                {
                    _aczSawmill.Error(
                        $"Exception in StatusHost PrepareACZip (server will not be usable from launcher!): {e}");
                    return null;
                }
            }
            finally
            {
                _acZipLock.Release();
            }
        }

        // -- All methods from this point forward do not access the ACZ global state --

        private AutomaticClientZipInfo? PrepareACZipInnards()
        {
            _aczSawmill.Info("Preparing ACZ...");
            // All of these should Info on success and Error on null-return failure
            var zipData = PrepareACZipViaFile() ?? PrepareACZipViaMagic();
            if (zipData == null)
                return null;

            var dataHash = Convert.ToHexString(SHA256.HashData(zipData));

            return new AutomaticClientZipInfo(
                zipData,
                dataHash);
        }

        private byte[]? PrepareACZipViaFile()
        {
            var path = PathHelpers.ExecutableRelativeFile("Content.Client.zip");
            if (!File.Exists(path)) return null;
            _aczSawmill.Info($"StatusHost found client zip: {path}");
            return File.ReadAllBytes(path);
        }

        private byte[]? PrepareACZipViaMagic()
        {
            var sw = Stopwatch.StartNew();

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

            var res = SSAZip.MakeZip(archive);
            _aczSawmill.Info("StatusHost synthesized client zip in {Elapsed} ms!", sw.ElapsedMilliseconds);
            return res;

            void AttemptPullFromDisk(string pathTo, string pathFrom)
            {
                // _aczSawmill.Debug($"StatusHost PrepareACZMagic: {pathFrom} -> {pathTo}");
                var res = PathHelpers.ExecutableRelativeFile(pathFrom);
                if (!File.Exists(res))
                    return;

                archive[pathTo] = new OnDemandDiskFile(res);
            }
        }

        public void SetAczInfo(string clientBinFolder, string[] clientAssemblyNames)
        {
            _acZipLock.Wait();
            try
            {
                if (_acZipPrepared != null)
                    throw new InvalidOperationException("ACZ already prepared");

                _aczInfo = (clientBinFolder, clientAssemblyNames);
            }
            finally
            {
                _acZipLock.Release();
            }
        }

        /// <param name="ZipData">Byte array containing the raw zip file data.</param>
        /// <param name="ZipHash">Hex SHA256 hash of <see cref="ZipData"/>.</param>
        internal sealed record AutomaticClientZipInfo(
            byte[] ZipData,
            string ZipHash);
    }
}
