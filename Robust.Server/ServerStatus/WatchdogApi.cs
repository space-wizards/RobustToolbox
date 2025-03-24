using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.ServerStatus
{
    internal sealed class WatchdogApi : IWatchdogApiInternal, IPostInjectInit
    {
        [Dependency] private readonly IStatusHost _statusHost = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IBaseServer _baseServer = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        // Ping watchdog every 15 seconds.
        private static readonly TimeSpan PingGap = TimeSpan.FromSeconds(15);
        private readonly HttpClient _httpClient = new(HappyEyeballsHttp.CreateHttpHandler());

        private TimeSpan? _lastPing;
        private string? _watchdogToken;
        private string? _watchdogKey;
        private Uri? _baseUri;
        private ISawmill _sawmill = default!;

        public WatchdogApi()
        {
            HttpClientUserAgent.AddUserAgent(_httpClient);
        }

        void IPostInjectInit.PostInject()
        {
            _sawmill = Logger.GetSawmill("watchdogApi");

            _statusHost.AddHandler(UpdateHandler);
            _statusHost.AddHandler(ShutdownHandler);
        }

        private async Task<bool> UpdateHandler(IStatusHandlerContext context)
        {
            if (context.RequestMethod != HttpMethod.Post || context.Url.AbsolutePath != "/update")
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
                _sawmill.Verbose(@"Failed auth: ""{0}"" vs ""{1}""", auth, _watchdogToken);
                await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
                return true;
            }

            RestartRequestParameters? parameters = null;
            if (context.RequestHeaders.TryGetValue("Content-Type", out var contentType)
                && contentType == MediaTypeNames.Application.Json)
            {
                try
                {
                    parameters = await context.RequestBodyJsonAsync<RestartRequestParameters>();
                }
                catch (JsonException)
                {
                    // parameters null so it'll catch the block down below.
                }

                if (parameters == null)
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }
            }

            RestartRequestedData restartData;
            if (parameters == null)
            {
                restartData = RestartRequestedData.DefaultData;
            }
            else
            {
                // Allow parsing to fail for forwards compatibility.
                var reasonCode = Enum.TryParse<RestartRequestedReason>(parameters.Reason, out var code)
                    ? code
                    : RestartRequestedReason.Other;

                restartData = new RestartRequestedData(reasonCode, parameters.Message);
            }

            _taskManager.RunOnMainThread(() =>
            {
                RestartRequested?.Invoke(restartData);
                UpdateReceived?.Invoke();
            });

            await context.RespondAsync("Success", HttpStatusCode.OK);

            return true;
        }

        /// <remarks>
        /// This function is used by https://github.com/tgstation/tgstation-server
        /// Notify the project maintainer(s) if this API is changed.
        /// </remarks>
        private async Task<bool> ShutdownHandler(IStatusHandlerContext context)
        {
            if (context.RequestMethod != HttpMethod.Post || context.Url.AbsolutePath != "/shutdown")
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
                await context.RespondAsync("Expected WatchdogToken header", HttpStatusCode.BadRequest);
                return true;
            }

            if (auth != _watchdogToken)
            {
                _sawmill.Verbose(
                    "received POST /shutdown with invalid authentication token. Ignoring {0}, {1}",
                    auth,
                    _watchdogToken);
                await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
                return true;
            }

            ShutdownParameters? parameters = null;
            try
            {
                parameters = await context.RequestBodyJsonAsync<ShutdownParameters>();
            }
            catch (JsonException)
            {
                // parameters null so it'll catch the block down below.
            }

            if (parameters == null)
            {
                await context.RespondErrorAsync(HttpStatusCode.BadRequest);

                return true;
            }

            _taskManager.RunOnMainThread(() => _baseServer.Shutdown(parameters.Reason));

            await context.RespondAsync("Success", HttpStatusCode.OK);

            return true;
        }

        public event Action? UpdateReceived;
        public event Action<RestartRequestedData>? RestartRequested;

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
                _sawmill.Debug("Sending ping to watchdog...");
                using var resp = await _httpClient.PostAsync(new Uri(_baseUri, $"server_api/{_watchdogKey}/ping"), null!);
                resp.EnsureSuccessStatusCode();
                _sawmill.Debug("Succeeded in sending ping to watchdog");
            }
            catch (HttpRequestException e)
            {
                _sawmill.Error("Failed to send ping to watchdog:\n{0}", e);
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

        [UsedImplicitly]
        private sealed class RestartRequestParameters
        {
            public string Reason { get; set; } = nameof(RestartRequestedReason.Other);
            public string? Message { get; set; }
        }
    }
}
