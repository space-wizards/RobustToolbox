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
    // Contains source logic for ACZ (Automatic Client Zip)
    // This entails the following:
    // * Automatic generation of client zip on development servers.
    // * Loading of pre-built client zip on release servers. ("Hybrid ACZ")

    internal sealed partial class StatusHost
    {
        private (string binFolder, string[] assemblies)? _aczInfo;

        // -- string path methods --

        private string? SourceACZipPathViaHybrid()
        {
            var path = PathHelpers.ExecutableRelativeFile("Content.Client.zip");
            if (!File.Exists(path)) return null;
            _aczSawmill.Info($"StatusHost found client zip: {path}");
            return path;
        }

        // -- byte[] methods --

        private byte[]? SourceACZip()
        {
            return SourceACZipViaFile() ?? SourceACZipViaMagic();
        }

        private byte[]? SourceACZipViaFile()
        {
            var path = SourceACZipPathViaHybrid();
            return path != null ? File.ReadAllBytes(path) : null;
        }

        private byte[]? SourceACZipViaMagic()
        {
            var sw = Stopwatch.StartNew();

            var archive = SourceACDictionaryViaMagic();
            if (archive == null)
                return null;
            var res = SSAZip.MakeZip(archive);

            _aczSawmill.Info("StatusHost synthesized client zip in {Elapsed} ms!", sw.ElapsedMilliseconds);
            return res;
        }

        // -- Dictionary<string, OnDemandFile> methods --

        private Dictionary<string, OnDemandFile>? SourceACDictionary()
        {
            return SourceACDictionaryViaExistingZip() ?? SourceACDictionaryViaFile() ?? SourceACDictionaryViaMagic();
        }

        private Dictionary<string, OnDemandFile>? SourceACDictionaryViaExistingZip()
        {
            // If we already generated or acquired a zip, there's no reason to read from disk again.
            // Opportunistically grab it.
            _acZipLock.Wait();
            byte[] zipData;
            try
            {
                if (_acZipPrepared == null) return null;
                zipData = _acZipPrepared.ZipData;
            }
            finally
            {
                _acZipLock.Release();
            }
            _aczSawmill.Info("StatusHost importing dictionary from existing zip...");
            var ms = new MemoryStream(zipData, false);
            return SourceACDictionaryViaZipStream(ms);
        }

        private Dictionary<string, OnDemandFile>? SourceACDictionaryViaFile()
        {
            var path = SourceACZipPathViaHybrid();
            if (path == null) return null;
            // Note: We don't want to explicitly close this, as the OnDemandFiles will hold references to this.
            // Let it be cleaned up by GC eventually.
            FileStream fs = File.OpenRead(path);
            return SourceACDictionaryViaZipStream(fs);
        }

        private Dictionary<string, OnDemandFile> SourceACDictionaryViaZipStream(Stream stream)
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

        private Dictionary<string, OnDemandFile>? SourceACDictionaryViaMagic()
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
            _acZipLock.Wait();
            try
            {
                _acManifestLock.Wait();
                try
                {
                    if (_acZipPrepared != null)
                        throw new InvalidOperationException("ACZip already prepared");
                    if (_acManifestPrepared != null)
                        throw new InvalidOperationException("ACManifest already prepared");

                    _aczInfo = (clientBinFolder, clientAssemblyNames);
                }
                finally
                {
                    _acManifestLock.Release();
                }
            }
            finally
            {
                _acZipLock.Release();
            }
        }
    }
}
