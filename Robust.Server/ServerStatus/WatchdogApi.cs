using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Robust.Server.Interfaces;
using Robust.Server.Interfaces.ServerStatus;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;

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
        private readonly HttpClient _httpClient = new HttpClient();

        private TimeSpan? _lastPing;
        private string? _watchdogToken;
        private string? _watchdogKey;
        private Uri? _baseUri;

        public void PostInject()
        {
            _statusHost.AddHandler(UpdateHandler);
            _statusHost.AddHandler(ShutdownHandler);

            _configurationManager.OnValueChanged(CVars.WatchdogToken, _ => UpdateToken());
            _configurationManager.OnValueChanged(CVars.WatchdogKey, _ => UpdateToken(), true);
        }

        private bool UpdateHandler(HttpMethod method, HttpRequest request, HttpResponse response)
        {
            if (method != HttpMethod.Post || request.Path != "/update")
            {
                return false;
            }

            if (_watchdogToken == null)
            {
                Logger.WarningS("watchdogApi", "Watchdog token is unset but received POST /update API call. Ignoring");
                return false;
            }

            var auth = request.Headers["WatchdogToken"];
            if (auth.Count != 1)
            {
                response.StatusCode = (int) HttpStatusCode.BadRequest;
                return true;
            }

            var authVal = auth[0];

            if (authVal != _watchdogToken)
            {
                // Holy shit nobody read these logs please.
                Logger.InfoS("watchdogApi", @"Failed auth: ""{0}"" vs ""{1}""", authVal, _watchdogToken);
                response.StatusCode = (int) HttpStatusCode.Unauthorized;
                return true;
            }

            _taskManager.RunOnMainThread(() => UpdateReceived?.Invoke());

            response.StatusCode = (int) HttpStatusCode.OK;

            return true;
        }

        private bool ShutdownHandler(HttpMethod method, HttpRequest request, HttpResponse response)
        {
            if (method != HttpMethod.Post || request.Path != "/shutdown")
            {
                return false;
            }

            if (_watchdogToken == null)
            {
                Logger.WarningS("watchdogApi",
                    "Watchdog token is unset but received POST /shutdown API call. Ignoring");
                return false;
            }

            var auth = request.Headers["WatchdogToken"];
            if (auth.Count != 1)
            {
                response.StatusCode = (int) HttpStatusCode.BadRequest;
                return true;
            }

            var authVal = auth[0];

            if (authVal != _watchdogToken)
            {
                Logger.WarningS("watchdogApi",
                    "received POST /shutdown with invalid authentication token. Ignoring {0}, {1}", authVal,
                    _watchdogToken);
                response.StatusCode = (int) HttpStatusCode.Unauthorized;
                return true;
            }


            ShutdownParameters? parameters = null;
            try
            {
                parameters = request.GetFromJson<ShutdownParameters>();
            }
            catch (JsonSerializationException)
            {
                // parameters null so it'll catch the block down below.
            }

            if (parameters == null)
            {
                response.StatusCode = (int) HttpStatusCode.BadRequest;

                return true;
            }

            _taskManager.RunOnMainThread(() => _baseServer.Shutdown(parameters.Reason));

            response.StatusCode = (int) HttpStatusCode.OK;

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
                await _httpClient.PostAsync(new Uri(_baseUri, $"server_api/{_watchdogKey}/ping"), null);
            }
            catch (HttpRequestException e)
            {
                Logger.WarningS("watchdogApi", "Failed to send ping to watchdog:\n{0}", e);
            }
        }

        public void Initialize()
        {
            UpdateToken();
        }

        private void UpdateToken()
        {
            var tok = _configurationManager.GetCVar<string>(CVars.WatchdogToken);
            var key = _configurationManager.GetCVar<string>(CVars.WatchdogKey);
            var baseUrl = _configurationManager.GetCVar<string>(CVars.WatchdogBaseUrl);
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
            [JsonProperty(Required = Required.Always)]
            public string Reason { get; set; } = default!;
        }
    }
}
