using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SS14.Server.Interfaces.Player;
using SS14.Server.Interfaces.ServerStatus;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Log;

// This entire file is NIHing a REST server because pulling in libraries is effort.
// Also it was fun to write.
// Just slap this thing behind an Nginx reverse proxy. It's not supposed to be directly exposed to the web.

namespace SS14.Server.ServerStatus
{
    public sealed class StatusHost : IStatusHost, IDisposable
    {
        [Dependency] private IConfigurationManager _configurationManager;

        // See this SO post for inspiration: https://stackoverflow.com/a/4673210

        private HttpListener _listener;
        private Thread _listenerThread;
        private ManualResetEventSlim _stop;

        public void Start()
        {
            _configurationManager.RegisterCVar("status.enabled", false, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("status.bind", "localhost:1212", CVar.ARCHIVE);

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
        }

        public event Action<JObject> OnStatusRequest;

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
            if (request.HttpMethod != "GET" && request.HttpMethod != "HEAD")
            {
                response.StatusCode = (int) HttpStatusCode.BadRequest;
                response.StatusDescription = "Bad Request";
                response.ContentType = "text/plain";
                _respondText(response, "400 Bad Request", false);
                return;
            }

            var head = request.HttpMethod == "HEAD";
            try
            {
                var uri = request.Url;
                if (uri.AbsolutePath == "/teapot")
                {
                    response.StatusCode = 418; // >HttpStatusCode doesn't include 418.
                    response.StatusDescription = "I'm a teapot";
                    response.ContentType = "text/plain";
                    _respondText(response, "The requested entity body is short and stout.", head);
                }
                else if (uri.AbsolutePath == "/status")
                {
                    if (OnStatusRequest == null)
                    {
                        response.StatusCode = (int) HttpStatusCode.NotImplemented;
                        response.StatusDescription = "Not Implemented";
                        response.ContentType = "text/plain";
                        _respondText(response, "501 Not Implemented", head);
                        Logger.WarningS("statushost", "OnStatusRequest is not set, responding with a 501.");
                        return;
                    }

                    response.StatusCode = (int) HttpStatusCode.OK;
                    response.StatusDescription = "OK";
                    response.ContentType = "application/json";
                    response.ContentEncoding = Encoding.UTF8;

                    if (head)
                    {
                        response.OutputStream.Close();
                        return;
                    }

                    var jObject = new JObject();
                    OnStatusRequest?.Invoke(jObject);
                    using (var streamWriter = new StreamWriter(response.OutputStream, Encoding.UTF8))
                    using (var jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        var serializer = new JsonSerializer();
                        serializer.Serialize(jsonWriter, jObject);
                        jsonWriter.Flush();
                    }

                    response.OutputStream.Close();
                }
                else
                {
                    response.StatusCode = (int) HttpStatusCode.NotFound;
                    response.StatusDescription = "Not Found";
                    response.ContentType = "text/plain";
                    _respondText(response, "404 Not Found", head);
                }
            }
            catch (Exception e)
            {
                response.StatusCode = (int) HttpStatusCode.InternalServerError;
                response.StatusDescription = "Internal Server Error";
                response.ContentType = "text/plain";
                _respondText(response, "500 Internal Server Error", head);
                Logger.ErrorS("statushost", "Exception in StatusHost: {0}", e);
            }
        }

        private static void _respondText(HttpListenerResponse response, string contents, bool head)
        {
            response.ContentEncoding = Encoding.UTF8;
            if (head)
            {
                response.OutputStream.Close();
                return;
            }

            using (var writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
            {
                writer.Write(contents);
            }

            response.OutputStream.Close();
        }
    }
}
