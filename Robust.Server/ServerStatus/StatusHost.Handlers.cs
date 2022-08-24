using System;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json.Nodes;
using Robust.Shared;

namespace Robust.Server.ServerStatus
{

    internal sealed partial class StatusHost
    {

        private void RegisterHandlers()
        {
            AddHandler(HandleTeapot);
            AddHandler(HandleStatus);
            AddHandler(HandleInfo);
            AddAczHandlers();
        }

        private static async Task<bool> HandleTeapot(IStatusHandlerContext context)
        {
            if (!context.IsGetLike || context.Url!.AbsolutePath != "/teapot")
            {
                return false;
            }

            await context.RespondAsync("I am a teapot.", (HttpStatusCode) 418);
            return true;
        }

        private async Task<bool> HandleStatus(IStatusHandlerContext context)
        {
            if (!context.IsGetLike || context.Url!.AbsolutePath != "/status")
            {
                return false;
            }

            var jObject = new JsonObject
            {
                // We need to send at LEAST name and player count to have the launcher work with us.
                // Tags is optional technically but will be necessary practically for future organization.
                // Content can override these if it wants (e.g. stealthmins).
                ["name"] = _serverNameCache,
                ["players"] = _playerManager.PlayerCount
            };

            var tagsCache = _serverTagsCache;
            if (tagsCache != null)
            {
                var tags = new JsonArray();
                foreach (var tag in tagsCache)
                {
                    tags.Add(tag);
                }
                jObject["tags"] = tags;
            }

            OnStatusRequest?.Invoke(jObject);

            await context.RespondJsonAsync(jObject);

            return true;
        }

        private async Task<bool> HandleInfo(IStatusHandlerContext context)
        {
            if (!context.IsGetLike || context.Url!.AbsolutePath != "/info")
            {
                return false;
            }

            var downloadUrl = _cfg.GetCVar(CVars.BuildDownloadUrl);

            JsonObject? buildInfo;

            if (string.IsNullOrEmpty(downloadUrl))
            {
                buildInfo = await PrepareACZBuildInfo();
            }
            else
            {
                buildInfo = GetExternalBuildInfo();
            }

            var authInfo = new JsonObject
            {
                ["mode"] = _netManager.Auth.ToString(),
                ["public_key"] = _netManager.CryptoPublicKey != null
                    ? Convert.ToBase64String(_netManager.CryptoPublicKey)
                    : null
            };

            var jObject = new JsonObject
            {
                ["connect_address"] = _cfg.GetCVar(CVars.StatusConnectAddress),
                ["auth"] = authInfo,
                ["build"] = buildInfo
            };

            OnInfoRequest?.Invoke(jObject);

            await context.RespondJsonAsync(jObject);

            return true;
        }

        private JsonObject GetExternalBuildInfo()
        {
            var zipHash = _cfg.GetCVar(CVars.BuildHash);
            var manifestHash = _cfg.GetCVar(CVars.BuildManifestHash);
            var forkId = _cfg.GetCVar(CVars.BuildForkId);
            var forkVersion = _cfg.GetCVar(CVars.BuildVersion);

            var manifestDownloadUrl = Interpolate(_cfg.GetCVar(CVars.BuildManifestDownloadUrl));
            var manifestUrl = Interpolate(_cfg.GetCVar(CVars.BuildManifestUrl));
            var downloadUrl = Interpolate(_cfg.GetCVar(CVars.BuildDownloadUrl));

            if (zipHash == "")
                zipHash = null;

            if (manifestHash == "")
                manifestHash = null;

            if (manifestDownloadUrl == "")
                manifestDownloadUrl = null;

            if (manifestUrl == "")
                manifestUrl = null;

            return new JsonObject
            {
                ["engine_version"] = _cfg.GetCVar(CVars.BuildEngineVersion),
                ["fork_id"] = forkId,
                ["version"] = forkVersion,
                ["download_url"] = downloadUrl,
                ["hash"] = zipHash,
                ["acz"] = false,
                ["manifest_download_url"] = manifestDownloadUrl,
                ["manifest_url"] = manifestUrl,
                ["manifest_hash"] = manifestHash
            };

            string? Interpolate(string? value)
            {
                // Can't tell if splitting the ?. like this is more cursed than
                // failing to align due to putting the full ?. on the next line
                return value?
                    .Replace("{FORK_VERSION}", forkVersion)
                    .Replace("{FORK_ID}", forkId)
                    .Replace("{MANIFEST_HASH}", manifestHash)
                    .Replace("{ZIP_HASH}", zipHash);
            }
        }

        private async Task<JsonObject?> PrepareACZBuildInfo()
        {
            var acm = await PrepareAcz();
            if (acm == null) return null;

            // Fork ID is an interesting case, we don't want to cause too many redownloads but we also don't want to pollute disk.
            // Call the fork "custom" if there's no explicit ID given.
            var fork = _cfg.GetCVar(CVars.BuildForkId);
            if (string.IsNullOrEmpty(fork))
            {
                fork = "custom";
            }
            return new JsonObject
            {
                ["engine_version"] = _cfg.GetCVar(CVars.BuildEngineVersion),
                ["fork_id"] = fork,
                ["version"] = acm.ManifestHash,
                // Don't supply a download URL - like supplying an empty self-address
                ["download_url"] = "",
                ["manifest_download_url"] = "",
                ["manifest_url"] = "",
                // Pass acz so the launcher knows where to find the downloads.
                ["acz"] = true,
                // Needs to be an empty 'hash' here or the launcher complains, this is from back when ACZ used zips
                ["hash"] = "",
                ["manifest_hash"] = acm.ManifestHash
            };
        }
    }

}
