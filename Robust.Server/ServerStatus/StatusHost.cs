using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Robust.Server.Interfaces.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

// This entire file is NIHing a REST server because pulling in libraries is effort.
// Also it was fun to write.
// Just slap this thing behind an Nginx reverse proxy. It's not supposed to be directly exposed to the web.

namespace Robust.Server.ServerStatus
{
    internal sealed class StatusHost : IStatusHost, IDisposable
    {
#pragma warning disable 649
        [Dependency] private IConfigurationManager _configurationManager;
#pragma warning restore 649

        // See this SO post for inspiration: https://stackoverflow.com/a/4673210

        private HttpListener _listener;
        private Thread _listenerThread;
        private ManualResetEventSlim _stop;
        public event Action<JObject> OnStatusRequest;
        public event Action<JObject> OnInfoRequest;

        private readonly List<StatusHostHandler> _handlers = new List<StatusHostHandler>();

        public void AddHandler(StatusHostHandler handler)
        {
            _handlers.Add(handler);
        }

        public void Start()
        {
            _configurationManager.RegisterCVar("status.enabled", true, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("status.bind", "localhost:1212", CVar.ARCHIVE);
            _configurationManager.RegisterCVar<string>("status.connectaddress", null, CVar.ARCHIVE);

            if (!_configurationManager.GetCVar<bool>("status.enabled"))
            {
                return;
            }

            _stop = new ManualResetEventSlim();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{_configurationManager.GetCVar<string>("status.bind")}/");
            _listener.Start();
            _listenerThread = new Thread(_worker)
            {
                Name = "REST API Thread",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _listenerThread.Start();

            AddHandler(_handleTeapot);
            AddHandler(_handleStatus);
            AddHandler(_handleInfo);
        }

        public void Dispose()
        {
            if (_stop == null)
            {
                return;
            }

            _stop.Set();
            _listenerThread.Join(1000);
            _listener.Stop();
        }

        private void _worker()
        {
            while (_listener.IsListening)
            {
                var context = _listener.BeginGetContext(ar =>
                {
                    var actualContext = _listener.EndGetContext(ar);
                    _processRequest(actualContext);
                }, null);

                if (0 == WaitHandle.WaitAny(new[] {_stop.WaitHandle, context.AsyncWaitHandle}))
                {
                    return;
                }
            }
        }

        private void _processRequest(HttpListenerContext context)
        {
            _processRequestInternal(context);
            Logger.DebugS("statushost", "{0} -> {1} {2}",
                context.Request.Url.AbsolutePath,
                context.Response.StatusCode,
                context.Response.StatusDescription);
        }

        private void _processRequestInternal(HttpListenerContext context)
        {
            var response = context.Response;
            var request = context.Request;
            var method = new HttpMethod(request.HttpMethod);

            try
            {
                foreach (var handler in _handlers)
                {
                    if (handler(method, request, response))
                    {
                        return;
                    }
                }

                // No handler returned true, assume no handlers care about this.
                // 404.
                response.Respond(method, "404 Not Found", HttpStatusCode.NotFound, "text/plain");
            }
            catch (Exception e)
            {
                response.Respond(method, "500 Internal Server Error", HttpStatusCode.InternalServerError, "text/plain");
                Logger.ErrorS("statushost", "Exception in StatusHost: {0}", e);
            }
        }


        private static bool _handleTeapot(HttpMethod method, HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!method.IsGetLike() || request.Url.AbsolutePath != "/teapot")
            {
                return false;
            }

            response.Respond(method, "The requested entity body is short and stout.", (HttpStatusCode) 418,
                "text/plain");
            return true;
        }

        private bool _handleStatus(HttpMethod method, HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!method.IsGetLike() || request.Url.AbsolutePath != "/status")
            {
                return false;
            }

            if (OnStatusRequest == null)
            {
                Logger.WarningS("statushost", "OnStatusRequest is not set, responding with a 501.");
                response.Respond(method, "", HttpStatusCode.NotImplemented, "text/plain");
                return true;
            }

            response.StatusCode = (int) HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.ContentType = "application/json";
            response.ContentEncoding = EncodingHelpers.UTF8;

            if (method == HttpMethod.Head)
            {
                response.Close();
                return true;
            }

            var jObject = new JObject();
            OnStatusRequest.Invoke(jObject);
            using (var streamWriter = new StreamWriter(response.OutputStream, EncodingHelpers.UTF8))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(jsonWriter, jObject);
                jsonWriter.Flush();
            }

            response.Close();

            return true;
        }

        private bool _handleInfo(HttpMethod method, HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!method.IsGetLike() || request.Url.AbsolutePath != "/info")
            {
                return false;
            }

            response.StatusCode = (int) HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.ContentType = "application/json";
            response.ContentEncoding = EncodingHelpers.UTF8;

            if (method == HttpMethod.Head)
            {
                response.Close();
                return true;
            }

            var jObject = new JObject
            {
                ["connect_address"] = _configurationManager.GetCVar<string>("status.connectaddress")
            };

            OnInfoRequest?.Invoke(jObject);

            using (var streamWriter = new StreamWriter(response.OutputStream, EncodingHelpers.UTF8))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(jsonWriter, jObject);
                jsonWriter.Flush();
            }

            response.Close();

            return true;
        }
    }
}
