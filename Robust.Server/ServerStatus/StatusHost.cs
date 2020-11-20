using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Robust.Server.Interfaces.ServerStatus;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;

// This entire file is NIHing a REST server because pulling in libraries is effort.
// Also it was fun to write.
// Just slap this thing behind an Nginx reverse proxy. It's not supposed to be directly exposed to the web.

namespace Robust.Server.ServerStatus
{

    internal sealed partial class StatusHost
        : IStatusHost, IDisposable,
            IHttpApplication<HttpContext>,
            IApplicationLifetime,
            ILoggerFactory
    {

        private const string Sawmill = "statushost";

        private static readonly JsonSerializer JsonSerializer = new();

        private readonly List<StatusHostHandler> _handlers = new();

        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;

        private KestrelServer _server = default!;

        public Task ProcessRequestAsync(HttpContext context)
        {
            var response = context.Response;
            var request = context.Request;
            var method = new HttpMethod(request.Method);
            InitHttpContextThread();

            Logger.InfoS(Sawmill, $"{method} {context.Request.Path} from " +
                $"{context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}");

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
                response.Respond("Not Found", HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                response.Respond("Internal Server Error", HttpStatusCode.InternalServerError);
                Logger.ErrorS(Sawmill, $"Exception in StatusHost: {e}");
            }

            Logger.DebugS(Sawmill, $"{method} {context.Request.Path} {context.Response.StatusCode} " +
                $"{(HttpStatusCode) context.Response.StatusCode} to {context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}");

            return Task.CompletedTask;
        }

        public event Action<JObject>? OnStatusRequest;

        public event Action<JObject>? OnInfoRequest;

        public void AddHandler(StatusHostHandler handler) => _handlers.Add(handler);

        public void Start()
        {
            RegisterCVars();

            if (!_configurationManager.GetCVar(CVars.StatusEnabled))
            {
                return;
            }

            ConfigureSawmills();

            _ctxFactory = CreateHttpContextFactory();

            var kestrelOpts = new KestrelServerOptions
            {
                AllowSynchronousIO = true,
                ApplicationSchedulingMode = SchedulingMode.ThreadPool
            };

            kestrelOpts.Listen(GetBinding());

            _server = new KestrelServer(
                Options.Create(
                    kestrelOpts
                ),
                GetSocketTransportFactory(),
                this
            );

            RegisterHandlers();

            _server.StartAsync(this, ApplicationStopping);

            _syncCtx = SynchronizationContext.Current!;

            if (_syncCtx == null)
            {
                SynchronizationContext.SetSynchronizationContext(_syncCtx = new SynchronizationContext());
            }
        }

        private IPEndPoint GetBinding()
        {
            var binding = _configurationManager.GetCVar(CVars.StatusBind).Split(':');
            var ipAddrStr = binding[0];
            if (ipAddrStr == "+" || ipAddrStr == "*")
            {
                ipAddrStr = "0.0.0.0";
            }

            var ipAddress = IPAddress.Parse(ipAddrStr);
            var port = int.Parse(binding[1]);
            var ipEndPoint = new IPEndPoint(ipAddress, port);
            return ipEndPoint;
        }

        private SocketTransportFactory GetSocketTransportFactory()
        {
            var transportFactory = new SocketTransportFactory(
                Options.Create(new SocketTransportOptions
                {
                    IOQueueCount = 42
                }),
                this,
                this
            );
            return transportFactory;
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
