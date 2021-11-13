using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Robust.Shared.Exceptions;
using HttpListener = ManagedHttpListener.HttpListener;
using HttpListenerContext = ManagedHttpListener.HttpListenerContext;

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
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private static readonly JsonSerializer JsonSerializer = new();
        private readonly List<StatusHostHandler> _handlers = new();
        private HttpListener? _listener;
        private TaskCompletionSource? _stopSource;
        private ISawmill _httpSawmill = default!;

        private string? _serverNameCache;

        public Task ProcessRequestAsync(HttpListenerContext context)
        {
            var apiContext = (IStatusHandlerContext) new ContextImpl(context);

            _httpSawmill.Info(
                $"{apiContext.RequestMethod} {apiContext.Url.PathAndQuery} from {apiContext.RemoteEndPoint}");

            try
            {
                foreach (var handler in _handlers)
                {
                    if (handler(apiContext))
                    {
                        return Task.CompletedTask;
                    }
                }

                // No handler returned true, assume no handlers care about this.
                // 404.
                apiContext.Respond("Not Found", HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                _httpSawmill.Error($"Exception in StatusHost: {e}");
                apiContext.Respond("Internal Server Error", HttpStatusCode.InternalServerError);
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

            // Cache this in a field to avoid thread safety shenanigans.
            // Writes/reads of references are atomic in C# so no further synchronization necessary.
            _configurationManager.OnValueChanged(CVars.GameHostName, n => _serverNameCache = n);

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
            // build.json starts here (returns on failure to find it)
            var path = PathHelpers.ExecutableRelativeFile("build.json");
            if (!File.Exists(path))
            {
                return;
            }

            var buildInfo = File.ReadAllText(path);
            var info = JsonConvert.DeserializeObject<BuildInfo>(buildInfo)!;

            // Don't replace cvars with contents of build.json if overriden by --cvar or such.
            SetCVarIfUnmodified(CVars.BuildEngineVersion, info.EngineVersion);
            SetCVarIfUnmodified(CVars.BuildForkId, info.ForkId);
            SetCVarIfUnmodified(CVars.BuildVersion, info.Version);
            SetCVarIfUnmodified(CVars.BuildDownloadUrl, info.Download ?? "");
            SetCVarIfUnmodified(CVars.BuildHash, info.Hash ?? "");

            void SetCVarIfUnmodified(CVarDef<string> cvar, string val)
            {
                if (_configurationManager.GetCVar(cvar) == "")
                    _configurationManager.SetCVar(cvar, val);
            }
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

        #pragma warning disable CS0649
        [JsonObject(ItemRequired = Required.DisallowNull)]
        private sealed class BuildInfo
        {
            [JsonProperty("engine_version")] public string EngineVersion = default!;
            [JsonProperty("hash")] public string? Hash;
            [JsonProperty("download")] public string? Download = default;
            [JsonProperty("fork_id")] public string ForkId = default!;
            [JsonProperty("version")] public string Version = default!;
        }
        #pragma warning restore CS0649

        private sealed class ContextImpl : IStatusHandlerContext
        {
            private readonly HttpListenerContext _context;
            public HttpMethod RequestMethod { get; }
            public IPEndPoint RemoteEndPoint => _context.Request.RemoteEndPoint!;
            public Uri Url => _context.Request.Url!;
            public bool IsGetLike => RequestMethod == HttpMethod.Head || RequestMethod == HttpMethod.Get;
            public IReadOnlyDictionary<string, StringValues> RequestHeaders { get; }

            public ContextImpl(HttpListenerContext context)
            {
                _context = context;
                RequestMethod = new HttpMethod(context.Request.HttpMethod!);

                var headers = new Dictionary<string, StringValues>();
                foreach (string? key in context.Request.Headers.Keys)
                {
                    if (key == null)
                        continue;

                    headers.Add(key, context.Request.Headers.GetValues(key));
                }

                RequestHeaders = headers;
            }

            [return: MaybeNull]
            public T RequestBodyJson<T>()
            {
                using var streamReader = new StreamReader(_context.Request.InputStream, EncodingHelpers.UTF8);
                using var jsonReader = new JsonTextReader(streamReader);

                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(jsonReader);
            }

            public void Respond(string text, HttpStatusCode code = HttpStatusCode.OK, string contentType = MediaTypeNames.Text.Plain)
            {
                Respond(text, (int) code, contentType);
            }

            public void Respond(string text, int code = 200, string contentType = MediaTypeNames.Text.Plain)
            {
                _context.Response.StatusCode = code;
                _context.Response.ContentType = contentType;

                if (RequestMethod == HttpMethod.Head)
                {
                    _context.Response.Close();
                    return;
                }

                using var writer = new StreamWriter(_context.Response.OutputStream, EncodingHelpers.UTF8);

                writer.Write(text);
            }

            public void Respond(byte[] data, HttpStatusCode code = HttpStatusCode.OK, string contentType = MediaTypeNames.Text.Plain)
            {
                Respond(data, (int) code, contentType);
            }

            public void Respond(byte[] data, int code = 200, string contentType = MediaTypeNames.Text.Plain)
            {
                _context.Response.StatusCode = code;
                _context.Response.ContentType = contentType;
                _context.Response.ContentLength64 = data.Length;

                if (RequestMethod == HttpMethod.Head)
                {
                    _context.Response.Close();
                    return;
                }

                _context.Response.Close(data, false);
            }

            public void RespondError(HttpStatusCode code)
            {
                Respond(code.ToString(), code);
            }

            public void RespondJson(object jsonData, HttpStatusCode code = HttpStatusCode.OK)
            {
                using var streamWriter = new StreamWriter(_context.Response.OutputStream, EncodingHelpers.UTF8);

                _context.Response.ContentType = MediaTypeNames.Application.Json;

                using var jsonWriter = new JsonTextWriter(streamWriter);

                JsonSerializer.Serialize(jsonWriter, jsonData);

                jsonWriter.Flush();
            }
        }
    }
}
