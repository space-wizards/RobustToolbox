using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        private static bool HandleTeapot(HttpMethod method, HttpRequest request, HttpResponse response)
        {
            if (!method.IsGetLike() || request.Path != "/teapot")
            {
                return false;
            }

            response.StatusCode = StatusCodes.Status418ImATeapot;
            response.Respond("I am a teapot.", StatusCodes.Status418ImATeapot);
            return true;
        }

        private bool HandleStatus(HttpMethod method, HttpRequest request, HttpResponse response)
        {
            if (!method.IsGetLike() || request.Path != "/status")
            {
                return false;
            }

            if (OnStatusRequest == null)
            {
                Logger.WarningS(Sawmill, "OnStatusRequest is not set, responding with a 501.");
                response.Respond("Not Implemented", HttpStatusCode.NotImplemented);
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

            using var streamWriter = new StreamWriter(response.Body, EncodingHelpers.UTF8);

            using var jsonWriter = new JsonTextWriter(streamWriter);

            JsonSerializer.Serialize(jsonWriter, jObject);

            jsonWriter.Flush();

            return true;
        }

        private bool HandleInfo(HttpMethod method, HttpRequest request, HttpResponse response)
        {
            if (!method.IsGetLike() || request.Path != "/info")
            {
                return false;
            }

            response.StatusCode = (int) HttpStatusCode.OK;
            response.ContentType = "application/json";

            if (method == HttpMethod.Head)
            {
                return true;
            }

            var downloadUrlWindows = _configurationManager.GetCVar<string>("build.download_url_windows");

            JObject buildInfo;

            if (downloadUrlWindows == null)
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
                        ["MacOS"] = _configurationManager.GetCVar<string>("build.download_url_macos"),
                        ["Linux"] = _configurationManager.GetCVar<string>("build.download_url_linux")
                    },
                    ["fork_id"] = _configurationManager.GetCVar<string>("build.fork_id"),
                    ["version"] = _configurationManager.GetCVar<string>("build.version"),
                    ["hashes"] = new JObject
                    {
                        ["Windows"] = _configurationManager.GetCVar<string>("build.hash_windows"),
                        ["MacOS"] = _configurationManager.GetCVar<string>("build.hash_macos"),
                        ["Linux"] = _configurationManager.GetCVar<string>("build.hash_linux"),
                    },
                };
            }

            var jObject = new JObject
            {
                ["connect_address"] = _configurationManager.GetCVar<string>("status.connectaddress"),
                ["build"] = buildInfo
            };

            OnInfoRequest?.Invoke(jObject);

            using var streamWriter = new StreamWriter(response.Body, EncodingHelpers.UTF8);

            using var jsonWriter = new JsonTextWriter(streamWriter);

            JsonSerializer.Serialize(jsonWriter, jObject);

            jsonWriter.Flush();

            return true;
        }

    }

}
