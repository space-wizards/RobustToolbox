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
    // Contains the zip-file logic for ACZ (Automatic Client Zip)
    // This entails the following:
    // * Distribution of zip files via status host, to facilitate easier server setup.
    // For the manifest system, see: StatusHost.ACManifest.cs
    // For sources of ACZ data, see: StatusHost.ACZSources.cs
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

            if (!_cfg.GetCVar(CVars.AczLegacyLauncherSupport))
            {
                await context.RespondAsync("ACZ legacy download has been disabled, update your launcher!", HttpStatusCode.NotFound);
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
            var zipData = SourceACZip();
            if (zipData == null)
            {
                _aczSawmill.Error("All ACZip methods failed.");
                return null;
            }

            var dataHash = Convert.ToHexString(SHA256.HashData(zipData));

            return new AutomaticClientZipInfo(
                zipData,
                dataHash);
        }

        /// <param name="ZipData">Byte array containing the raw zip file data.</param>
        /// <param name="ZipHash">Hex SHA256 hash of <see cref="ZipData"/>.</param>
        internal sealed record AutomaticClientZipInfo(
            byte[] ZipData,
            string ZipHash);
    }
}
