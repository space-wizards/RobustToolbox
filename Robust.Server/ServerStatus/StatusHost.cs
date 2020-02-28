using System;
using System.Collections.Generic;
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
using Robust.Shared.Configuration;
using Robust.Shared.Interfaces.Configuration;
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

        private static readonly JsonSerializer JsonSerializer = new JsonSerializer();

        private readonly List<StatusHostHandler> _handlers = new List<StatusHostHandler>();

#pragma warning disable 649
        [Dependency] private IConfigurationManager _configurationManager;
#pragma warning restore 649

        private KestrelServer _server;

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

        public event Action<JObject> OnStatusRequest;

        public event Action<JObject> OnInfoRequest;

        public void AddHandler(StatusHostHandler handler) => _handlers.Add(handler);

        public void Start()
        {
            RegisterCVars();

            if (!_configurationManager.GetCVar<bool>("status.enabled"))
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

            _syncCtx = SynchronizationContext.Current;

            if (_syncCtx == null)
            {
                SynchronizationContext.SetSynchronizationContext(_syncCtx = new SynchronizationContext());
            }
        }

        private IPEndPoint GetBinding()
        {
            var binding = _configurationManager.GetCVar<string>("status.bind").Split(':');
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
            _configurationManager.RegisterCVar("status.enabled", true, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("status.bind", "*:1212", CVar.ARCHIVE);
            _configurationManager.RegisterCVar<string>("status.connectaddress", null, CVar.ARCHIVE);

            _configurationManager.RegisterCVar("build.fork_id", (string) null, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("build.version", (string) null, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("build.download_url_windows", (string) null, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("build.download_url_macos", (string) null, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("build.download_url_linux", (string) null, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("build.hash_windows", (string) null, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("build.hash_macos", (string) null, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("build.hash_linux", (string) null, CVar.ARCHIVE);
        }

    }

}
