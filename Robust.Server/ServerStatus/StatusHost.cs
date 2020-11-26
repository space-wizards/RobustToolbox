using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Robust.Server.Interfaces.ServerStatus;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;

// This entire file is NIHing a REST server because pulling in libraries is effort.
// Also it was fun to write.
// Just slap this thing behind an Nginx reverse proxy. It's not supposed to be directly exposed to the web.

namespace Robust.Server.ServerStatus
{
    internal sealed partial class StatusHost : IStatusHost, IDisposable
    {
        private const string Sawmill = "statushost";

        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;

        private static readonly JsonSerializer JsonSerializer = new();
        private readonly List<StatusHostHandler> _handlers = new();
        private HttpListener? _listener;
        private TaskCompletionSource? _stopSource;
        private ISawmill _httpSawmill = default!;

        public Task ProcessRequestAsync(HttpListenerContext context)
        {
            var response = context.Response;
            var request = context.Request;
            var method = new HttpMethod(request.HttpMethod);

            _httpSawmill.Info($"{method} {context.Request.Url?.PathAndQuery} from {request.RemoteEndPoint}");

            try
            {
                foreach (var handler in _handlers)
                {
                    if (handler(method, request, response))
                    {
                        return Task.CompletedTask;
                    }
                }

                // No handler returned true, assume no handlers care about this.
                // 404.
                response.Respond(method, "Not Found", HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                response.Respond(method, "Internal Server Error", HttpStatusCode.InternalServerError);
                Logger.ErrorS(Sawmill, $"Exception in StatusHost: {e}");
            }

            /*
            _httpSawmill.Debug(Sawmill, $"{method} {context.Request.Url!.PathAndQuery} {context.Response.StatusCode} " +
                                         $"{(HttpStatusCode) context.Response.StatusCode} to {context.Request.RemoteEndPoint}");
                                         */

            return Task.CompletedTask;
        }

        public event Action<JObject>? OnStatusRequest;

        public event Action<JObject>? OnInfoRequest;

        public void AddHandler(StatusHostHandler handler)
        {
            _handlers.Add(handler);
        }

        public void Start()
        {
            _httpSawmill = Logger.GetSawmill($"{Sawmill}.http");
            RegisterCVars();

            if (!_configurationManager.GetCVar(CVars.StatusEnabled))
            {
                return;
            }

            RegisterHandlers();

            _stopSource = new TaskCompletionSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{_configurationManager.GetCVar(CVars.StatusBind)}/");
            _listener.Start();

            Task.Run(ListenerThread);
        }

        // Not a real thread but whatever.
        private async Task ListenerThread()
        {
            var maxConnections = _configurationManager.GetCVar(CVars.StatusMaxConnections);
            var connectionsSemaphore = new SemaphoreSlim(maxConnections, maxConnections);
            while (true)
            {
                var getContextTask = _listener!.GetContextAsync();
                var task = await Task.WhenAny(getContextTask, _stopSource!.Task);

                if (task == _stopSource.Task)
                {
                    return;
                }

                await connectionsSemaphore.WaitAsync();

                // Task.Run this so it gets run on another thread pool thread.
#pragma warning disable 4014
                Task.Run(async () =>
#pragma warning restore 4014
                {
                    try
                    {
                        var ctx = await getContextTask;
                        await ProcessRequestAsync(ctx);
                    }
                    catch (Exception e)
                    {
                        _httpSawmill.Error($"Error inside ProcessRequestAsync:\n{e}");
                    }
                    finally
                    {
                        connectionsSemaphore.Release();
                    }
                });
            }
        }

        private void RegisterCVars()
        {
            BuildInfo? info = null;
            try
            {
                var buildInfo = File.ReadAllText(PathHelpers.ExecutableRelativeFile("build.json"));
                info = JsonConvert.DeserializeObject<BuildInfo>(buildInfo);
            }
            catch (FileNotFoundException)
            {
            }

            _configurationManager.SetCVar(CVars.BuildForkId, info?.ForkId ?? "");
            _configurationManager.SetCVar(CVars.BuildVersion, info?.Version ?? "");
            _configurationManager.SetCVar(CVars.BuildDownloadUrlWindows, info?.Downloads.Windows ?? "");
            _configurationManager.SetCVar(CVars.BuildDownloadUrlMacOS, info?.Downloads.MacOS ?? "");
            _configurationManager.SetCVar(CVars.BuildDownloadUrlLinux, info?.Downloads.Linux ?? "");
            _configurationManager.SetCVar(CVars.BuildHashWindows, info?.Hashes.Windows ?? "");
            _configurationManager.SetCVar(CVars.BuildHashMacOS, info?.Hashes.MacOS ?? "");
            _configurationManager.SetCVar(CVars.BuildHashLinux, info?.Hashes.Linux ?? "");
        }

        public void Dispose()
        {
            if (_stopSource == null)
            {
                return;
            }

            _stopSource.SetResult();
            _listener!.Stop();
        }

        [JsonObject(ItemRequired = Required.DisallowNull)]
        private sealed class BuildInfo
        {
            [JsonProperty("hashes")] public PlatformData Hashes { get; set; } = default!;
            [JsonProperty("downloads")] public PlatformData Downloads { get; set; } = default!;
            [JsonProperty("fork_id")] public string ForkId { get; set; } = default!;
            [JsonProperty("version")] public string Version { get; set; } = default!;
        }

        [JsonObject(ItemRequired = Required.DisallowNull)]
        private sealed class PlatformData
        {
            [JsonProperty("windows")] public string Windows { get; set; } = default!;
            [JsonProperty("linux")] public string Linux { get; set; } = default!;
            [JsonProperty("macos")] public string MacOS { get; set; } = default!;
        }
    }
}
