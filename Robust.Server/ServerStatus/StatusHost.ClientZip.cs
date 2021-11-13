using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Server.ServerStatus
{

    internal sealed partial class StatusHost
    {
        // Lock used while working on the ACZ.
        private readonly object _aczLock = new();
        // If an attempt has been made to prepare the ACZ.
        private bool _aczPrepareAttempted = false;
        // Automatic Client Zip
        private byte[]? _aczData;
        private string _aczHash = "";

        private bool HandleAutomaticClientZip(IStatusHandlerContext context)
        {
            if (!context.IsGetLike || context.Url!.AbsolutePath != "/acz.zip")
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_configurationManager.GetCVar(CVars.BuildDownloadUrl)))
            {
                context.Respond("This server has a build download URL.", HttpStatusCode.NotFound);
                return true;
            }

            var result = PrepareACZ();
            if (result == null)
            {
                context.Respond("Automatic Client Zip was not preparable.", HttpStatusCode.InternalServerError);
                return true;
            }

            context.Respond(result, HttpStatusCode.OK, "application/zip");
            return true;
        }

        private byte[]? PrepareACZ()
        {
            lock (_aczLock)
            {
                if (_aczPrepareAttempted) return _aczData;
                _aczPrepareAttempted = true;
                byte[] data;
                try
                {
                    var maybeData = PrepareACZInnards();
                    if (maybeData == null)
                    {
                        return null;
                    }
                    data = maybeData;
                }
                catch (Exception e)
                {
                    _httpSawmill.Error($"Exception in StatusHost PrepareACZ: {e}");
                    return null;
                }
                _aczData = data;
                using var sha = SHA256.Create();
                _aczHash = Convert.ToHexString(sha.ComputeHash(data));
                return data;
            }
        }

        private byte[]? PrepareACZInnards()
        {
            return PrepareACZViaFile() ?? PrepareACZViaMagic();
        }

        private byte[]? PrepareACZViaFile()
        {
            var path = PathHelpers.ExecutableRelativeFile("Content.Client.zip");
            if (!File.Exists(path)) return null;
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
            AttemptPullFromDisk("Assemblies/Content.Shared.dll", "../../bin/Content.Client/Content.Shared.dll");
            AttemptPullFromDisk("Assemblies/Content.Shared.pdb", "../../bin/Content.Client/Content.Shared.pdb");
            if (!AttemptPullFromDisk("Assemblies/Content.Client.dll", "../../bin/Content.Client/Content.Client.dll"))
            {
                _httpSawmill.Error($"StatusHost PrepareACZMagic couldn't get client assembly - not continuing");
                return null;
            }
            AttemptPullFromDisk("Assemblies/Content.Client.pdb", "../../bin/Content.Client/Content.Client.pdb");

            var prefix = PathHelpers.ExecutableRelativeFile("../../Resources");
            foreach (var path in PathHelpers.GetFiles(prefix))
            {
                var relPath = Path.GetRelativePath(prefix, path);
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
            return outStream.ToArray();
        }
    }

}
