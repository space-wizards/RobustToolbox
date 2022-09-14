using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using HttpListener = SpaceWizards.HttpListener.HttpListener;
using HttpListenerContext = SpaceWizards.HttpListener.HttpListenerContext;

// This entire file is NIHing a REST server because pulling in libraries is effort.
// Also it was fun to write.
// Just slap this thing behind an Nginx reverse proxy. It's not supposed to be directly exposed to the web.

namespace Robust.Server.ServerStatus
{
    internal sealed partial class StatusHost : IStatusHost, IDisposable
    {
        private const string Sawmill = "statushost";

        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IDependencyCollection _deps = default!;

        private readonly List<StatusHostHandlerAsync> _handlers = new();
        private HttpListener? _listener;
        private TaskCompletionSource? _stopSource;
        private ISawmill _httpSawmill = default!;
        private ISawmill _aczSawmill = default!;
        private ISawmill _aczPackagingSawmill = default!;

        private string? _serverNameCache;
        private string[]? _serverTagsCache;

        public async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var apiContext = (IStatusHandlerContext) new ContextImpl(context);

            _httpSawmill.Info(
                $"{apiContext.RequestMethod} {apiContext.Url.PathAndQuery} from {apiContext.RemoteEndPoint}");

            try
            {
                foreach (var handler in _handlers)
                {
                    if (await handler(apiContext))
                    {
                        return;
                    }
                }

                // No handler returned true, assume no handlers care about this.
                // 404.
                await apiContext.RespondAsync("Not Found", HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                _httpSawmill.Error($"Exception in StatusHost: {e}");
                await apiContext.RespondErrorAsync(HttpStatusCode.InternalServerError);
            }

            /*
            _httpSawmill.Debug(Sawmill, $"{method} {context.Request.Url!.PathAndQuery} {context.Response.StatusCode} " +
                                         $"{(HttpStatusCode) context.Response.StatusCode} to {context.Request.RemoteEndPoint}");
                                         */
        }

        public event Action<JsonNode>? OnStatusRequest;

        public event Action<JsonNode>? OnInfoRequest;

        // TODO: Remove at some point in the future
#pragma warning disable CS0618 // Uses the obsolete StatusHostHandler. Exists for backwards compatibility
        public void AddHandler(StatusHostHandler handler)
        {
            _handlers.Add((ctx) => Task.FromResult(handler(ctx)));
        }
#pragma warning restore CS0618

        public void AddHandler(StatusHostHandlerAsync handler)
        {
            _handlers.Add(handler);
        }

        public void Start()
        {
            _httpSawmill = Logger.GetSawmill($"{Sawmill}.http");
            _aczSawmill = Logger.GetSawmill($"{Sawmill}.acz");
            _aczPackagingSawmill = Logger.GetSawmill($"{Sawmill}.acz.packaging");

            RegisterCVars();

            // Cache these in fields to avoid thread safety shenanigans.
            // Writes/reads of references are atomic in C# so no further synchronization necessary.
            _cfg.OnValueChanged(CVars.GameHostName, n => _serverNameCache = n, true);
            _cfg.OnValueChanged(CVars.HubTags, t =>
                {
                    var tags = t.Split(",", StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 0; i < tags.Length; i++)
                    {
                        tags[i] = tags[i].Trim();
                    }
                    _serverTagsCache = tags;
                },
                true
            );

            if (!_cfg.GetCVar(CVars.StatusEnabled))
            {
                return;
            }

            RegisterHandlers();

            _stopSource = new TaskCompletionSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{_cfg.GetCVar(CVars.StatusBind)}/");
            _listener.Start();

            Task.Run(ListenerThread);
        }

        // Not a real thread but whatever.
        private async Task ListenerThread()
        {
            var maxConnections = _cfg.GetCVar(CVars.StatusMaxConnections);
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
            InitAcz();

            // Set status host binding to match network manager by default
            SetCVarIfUnmodified(CVars.StatusBind, $"*:{_netManager.Port}");

            // Check build.json
            var path = PathHelpers.ExecutableRelativeFile("build.json");
            if (File.Exists(path))
            {
                var buildInfo = File.ReadAllText(path);
                var info = JsonSerializer.Deserialize<BuildInfo>(buildInfo)!;

                // Don't replace cvars with contents of build.json if overriden by --cvar or such.
                SetCVarIfUnmodified(CVars.BuildEngineVersion, info.EngineVersion);
                SetCVarIfUnmodified(CVars.BuildForkId, info.ForkId);
                SetCVarIfUnmodified(CVars.BuildVersion, info.Version);
                SetCVarIfUnmodified(CVars.BuildDownloadUrl, info.Download ?? "");
                SetCVarIfUnmodified(CVars.BuildHash, info.Hash ?? "");
                SetCVarIfUnmodified(CVars.BuildManifestHash, info.ManifestHash ?? "");
                SetCVarIfUnmodified(CVars.BuildManifestDownloadUrl, info.ManifestDownloadUrl ?? "");
                SetCVarIfUnmodified(CVars.BuildManifestUrl, info.ManifestUrl ?? "");
            }

            // Automatically determine engine version if no other source has provided a result
            var asmVer = typeof(StatusHost).Assembly.GetName().Version;
            if (asmVer != null)
            {
                SetCVarIfUnmodified(CVars.BuildEngineVersion, asmVer.ToString(4));
            }

            void SetCVarIfUnmodified(CVarDef<string> cvar, string val)
            {
                if (_cfg.GetCVar(cvar) == "")
                    _cfg.SetCVar(cvar, val);
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

        private sealed record BuildInfo(
            [property: JsonPropertyName("engine_version")]
            string EngineVersion,
            [property: JsonPropertyName("hash")]
            string? Hash,
            [property: JsonPropertyName("download")]
            string? Download,
            [property: JsonPropertyName("fork_id")]
            string ForkId,
            [property: JsonPropertyName("version")]
            string Version,
            [property: JsonPropertyName("manifest_hash")]
            string? ManifestHash,
            [property: JsonPropertyName("manifest_url")]
            string? ManifestUrl,
            [property: JsonPropertyName("manifest_download_url")]
            string? ManifestDownloadUrl);

        private sealed class ContextImpl : IStatusHandlerContext
        {
            private readonly HttpListenerContext _context;
            private readonly Dictionary<string, string> _responseHeaders;
            public HttpMethod RequestMethod { get; }
            public IPEndPoint RemoteEndPoint => _context.Request.RemoteEndPoint!;
            public Stream RequestBody => _context.Request.InputStream;
            public Uri Url => _context.Request.Url!;
            public bool IsGetLike => RequestMethod == HttpMethod.Head || RequestMethod == HttpMethod.Get;
            public IReadOnlyDictionary<string, StringValues> RequestHeaders { get; }

            public bool KeepAlive
            {
                get => _context.Response.KeepAlive;
                set => _context.Response.KeepAlive = value;
            }

            public IDictionary<string, string> ResponseHeaders => _responseHeaders;

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
                _responseHeaders = new Dictionary<string, string>();
            }

            public T? RequestBodyJson<T>()
            {
                return JsonSerializer.Deserialize<T>(RequestBody);
            }

            public async Task<T?> RequestBodyJsonAsync<T>()
            {
                return await JsonSerializer.DeserializeAsync<T>(RequestBody);
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

                _context.Response.OutputStream.Write(data);
                _context.Response.Close();
            }

            public Task RespondNoContentAsync()
            {
                RespondShared();

                _context.Response.StatusCode = (int) HttpStatusCode.NoContent;
                _context.Response.Close();

                return Task.CompletedTask;
            }

            public Task RespondAsync(string text, HttpStatusCode code = HttpStatusCode.OK, string contentType = "text/plain")
            {
                return RespondAsync(text, (int) code, contentType);
            }

            public async Task RespondAsync(string text, int code = 200, string contentType = "text/plain")
            {
                RespondShared();

                _context.Response.StatusCode = code;
                _context.Response.ContentType = contentType;

                if (RequestMethod == HttpMethod.Head)
                    return;

                using var writer = new StreamWriter(_context.Response.OutputStream, EncodingHelpers.UTF8);

                await writer.WriteAsync(text);
            }

            public Task RespondAsync(byte[] data, HttpStatusCode code = HttpStatusCode.OK, string contentType = "text/plain")
            {
                return RespondAsync(data, (int) code, contentType);
            }

            public async Task RespondAsync(byte[] data, int code = 200, string contentType = "text/plain")
            {
                RespondShared();

                _context.Response.StatusCode = code;
                _context.Response.ContentType = contentType;
                _context.Response.ContentLength64 = data.Length;

                if (RequestMethod == HttpMethod.Head)
                {
                    _context.Response.Close();
                    return;
                }

                await _context.Response.OutputStream.WriteAsync(data);
                _context.Response.Close();
            }

            public void RespondError(HttpStatusCode code)
            {
                Respond(code.ToString(), code);
            }

            public Task RespondErrorAsync(HttpStatusCode code)
            {
                return RespondAsync(code.ToString(), code);
            }

            public void RespondJson(object jsonData, HttpStatusCode code = HttpStatusCode.OK)
            {
                RespondShared();

                _context.Response.ContentType = "application/json";

                JsonSerializer.Serialize(_context.Response.OutputStream, jsonData);

                _context.Response.Close();
            }

            public async Task RespondJsonAsync(object jsonData, HttpStatusCode code = HttpStatusCode.OK)
            {
                RespondShared();

                _context.Response.ContentType = "application/json";

                await JsonSerializer.SerializeAsync(_context.Response.OutputStream, jsonData);

                _context.Response.Close();
            }

            public Task<Stream> RespondStreamAsync(HttpStatusCode code = HttpStatusCode.OK)
            {
                RespondShared();

                _context.Response.StatusCode = (int) code;

                return Task.FromResult(_context.Response.OutputStream);
            }

            private void RespondShared()
            {
                foreach (var (header, value) in _responseHeaders)
                {
                    _context.Response.AddHeader(header, value);
                }
            }
        }
    }
}
