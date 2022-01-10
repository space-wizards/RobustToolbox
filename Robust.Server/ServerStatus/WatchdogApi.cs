using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

#nullable enable

namespace Robust.Server.ServerStatus
{
    public class WatchdogApi : IWatchdogApi, IPostInjectInit
    {
        [Dependency] private readonly IStatusHost _statusHost = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IBaseServer _baseServer = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        // Ping watchdog every 15 seconds.
        private static readonly TimeSpan PingGap = TimeSpan.FromSeconds(15);
        private readonly HttpClient _httpClient = new();

        private TimeSpan? _lastPing;
        private string? _watchdogToken;
        private string? _watchdogKey;
        private Uri? _baseUri;
        private ISawmill _sawmill = default!;

        public void PostInject()
        {
            _sawmill = Logger.GetSawmill("watchdogApi");

            _statusHost.AddHandler(UpdateHandler);
            _statusHost.AddHandler(ShutdownHandler);
        }

        private bool UpdateHandler(IStatusHandlerContext context)
        {
            if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/update")
            {
                return false;
            }

            if (_watchdogToken == null)
            {
                _sawmill.Warning("Watchdog token is unset but received POST /update API call. Ignoring");
                return false;
            }

            var auth = context.RequestHeaders["WatchdogToken"];

            if (auth != _watchdogToken)
            {
                // Holy shit nobody read these logs please.
                _sawmill.Info(@"Failed auth: ""{0}"" vs ""{1}""", auth, _watchdogToken);
                context.RespondError(HttpStatusCode.Unauthorized);
                return true;
            }

            _taskManager.RunOnMainThread(() => UpdateReceived?.Invoke());

            context.Respond("Success", HttpStatusCode.OK);

            return true;
        }

        private bool ShutdownHandler(IStatusHandlerContext context)
        {
            if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/shutdown")
            {
                return false;
            }

            if (_watchdogToken == null)
            {
                _sawmill.Warning("Watchdog token is unset but received POST /shutdown API call. Ignoring");
                return false;
            }

            if (!context.RequestHeaders.TryGetValue("WatchdogToken", out var auth))
            {
                context.Respond("Expected WatchdogToken header", HttpStatusCode.BadRequest);
                return true;
            }

            if (auth != _watchdogToken)
            {
                _sawmill.Warning(
                    "received POST /shutdown with invalid authentication token. Ignoring {0}, {1}", auth,
                    _watchdogToken);
                context.RespondError(HttpStatusCode.Unauthorized);
                return true;
            }

            ShutdownParameters? parameters = null;
            try
            {
                parameters = context.RequestBodyJson<ShutdownParameters>();
            }
            catch (JsonException)
            {
                // parameters null so it'll catch the block down below.
            }

            if (parameters == null)
            {
                context.RespondError(HttpStatusCode.BadRequest);

                return true;
            }

            _taskManager.RunOnMainThread(() => _baseServer.Shutdown(parameters.Reason));

            context.Respond("Success", HttpStatusCode.OK);

            return true;
        }

        public event Action? UpdateReceived;

        public async void Heartbeat()
        {
            if (_watchdogToken == null || _watchdogKey == null || _baseUri == null)
            {
                return;
            }

            // Ping upon startup to indicate successful init.
            var realTime = _gameTiming.RealTime;
            if (_lastPing.HasValue && realTime - _lastPing < PingGap)
            {
                return;
            }

            _lastPing = realTime;

            try
            {
                // Passing null as content works so...
                await _httpClient.PostAsync(new Uri(_baseUri, $"server_api/{_watchdogKey}/ping"), null!);
            }
            catch (HttpRequestException e)
            {
                Logger.WarningS("watchdogApi", "Failed to send ping to watchdog:\n{0}", e);
            }
        }

        public void Initialize()
        {
            _configurationManager.OnValueChanged(CVars.WatchdogToken, _ => UpdateToken());
            _configurationManager.OnValueChanged(CVars.WatchdogKey, _ => UpdateToken());

            UpdateToken();
        }

        private void UpdateToken()
        {
            var tok = _configurationManager.GetCVar(CVars.WatchdogToken);
            var key = _configurationManager.GetCVar(CVars.WatchdogKey);
            var baseUrl = _configurationManager.GetCVar(CVars.WatchdogBaseUrl);
            _watchdogToken = string.IsNullOrEmpty(tok) ? null : tok;
            _watchdogKey = string.IsNullOrEmpty(key) ? null : key;
            _baseUri = string.IsNullOrEmpty(baseUrl) ? null : new Uri(baseUrl);

            if (_watchdogKey != null && _watchdogToken != null)
            {
                var paramStr = $"{_watchdogKey}:{_watchdogToken}";
                var param = Convert.ToBase64String(Encoding.UTF8.GetBytes(paramStr));

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", param);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        [UsedImplicitly]
        private sealed class ShutdownParameters
        {
            // ReSharper disable once RedundantDefaultMemberInitializer
            public string Reason { get; set; } = default!;
        }
    }
}
