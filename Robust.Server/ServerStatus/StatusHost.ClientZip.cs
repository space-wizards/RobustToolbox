using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Robust.Shared.Log;

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
                        Logger.WarningS("r.s.serverstatus.clientzip", "Unable to prepare hosted client-zip.");
                        return null;
                    }
                    data = maybeData;
                }
                catch (Exception e)
                {
                    _runtimeLog.LogException(e, "statushostacz");
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
            // NYI
            return null;
        }
    }

}
