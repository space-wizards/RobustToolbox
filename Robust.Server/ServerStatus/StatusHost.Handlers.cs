using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Robust.Shared;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Server.ServerStatus
{

    internal sealed partial class StatusHost
    {

        private void RegisterHandlers()
        {
            AddHandler(HandleTeapot);
            AddHandler(HandleStatus);
            AddHandler(HandleInfo);
        }

        private static bool HandleTeapot(HttpMethod method, HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!method.IsGetLike() || request.Url!.AbsolutePath != "/teapot")
            {
                return false;
            }

            response.Respond(method, "I am a teapot.", (HttpStatusCode) 418);
            return true;
        }

        private bool HandleStatus(HttpMethod method, HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!method.IsGetLike() || request.Url!.AbsolutePath != "/status")
            {
                return false;
            }

            if (OnStatusRequest == null)
            {
                Logger.WarningS(Sawmill, "OnStatusRequest is not set, responding with a 501.");
                response.Respond(method, "Not Implemented", HttpStatusCode.NotImplemented);
                return true;
            }

            response.StatusCode = (int) HttpStatusCode.OK;
            response.ContentType = "application/json";

            if (method == HttpMethod.Head)
            {
                return true;
            }

            var jObject = new JObject();

            OnStatusRequest?.Invoke(jObject);

            using var streamWriter = new StreamWriter(response.OutputStream, EncodingHelpers.UTF8);

            using var jsonWriter = new JsonTextWriter(streamWriter);

            JsonSerializer.Serialize(jsonWriter, jObject);

            jsonWriter.Flush();

            return true;
        }

        private bool HandleInfo(HttpMethod method, HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!method.IsGetLike() || request.Url!.AbsolutePath != "/info")
            {
                return false;
            }

            response.StatusCode = (int) HttpStatusCode.OK;
            response.ContentType = "application/json";

            if (method == HttpMethod.Head)
            {
                return true;
            }

            var downloadUrlWindows = _configurationManager.GetCVar(CVars.BuildDownloadUrlWindows);

            JObject? buildInfo;

            if (string.IsNullOrEmpty(downloadUrlWindows))
            {
                buildInfo = null;
            }
            else
            {
                buildInfo = new JObject
                {
                    ["download_urls"] = new JObject
                    {
                        ["Windows"] = downloadUrlWindows,
                        ["MacOS"] = _configurationManager.GetCVar(CVars.BuildDownloadUrlMacOS),
                        ["Linux"] = _configurationManager.GetCVar(CVars.BuildDownloadUrlLinux)
                    },
                    ["fork_id"] = _configurationManager.GetCVar(CVars.BuildForkId),
                    ["version"] = _configurationManager.GetCVar(CVars.BuildVersion),
                    ["hashes"] = new JObject
                    {
                        ["Windows"] = _configurationManager.GetCVar(CVars.BuildHashWindows),
                        ["MacOS"] = _configurationManager.GetCVar(CVars.BuildHashMacOS),
                        ["Linux"] = _configurationManager.GetCVar(CVars.BuildHashLinux),
                    },
                };
            }

            var authInfo = new JObject
            {
                ["mode"] = _netManager.Auth.ToString(),
                ["public_key"] = _netManager.RsaPublicKey != null
                    ? Convert.ToBase64String(_netManager.RsaPublicKey)
                    : null
            };

            var jObject = new JObject
            {
                ["connect_address"] = _configurationManager.GetCVar(CVars.StatusConnectAddress),
                ["auth"] = authInfo,
                ["build"] = buildInfo
            };

            OnInfoRequest?.Invoke(jObject);

            using var streamWriter = new StreamWriter(response.OutputStream, EncodingHelpers.UTF8);

            using var jsonWriter = new JsonTextWriter(streamWriter);

            JsonSerializer.Serialize(jsonWriter, jObject);

            jsonWriter.Flush();

            return true;
        }
    }

}
