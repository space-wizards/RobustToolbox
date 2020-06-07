using System;
using Prometheus;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;

#nullable enable

namespace Robust.Server.DataMetrics
{
    internal sealed class MetricsManager : IMetricsManager, IPostInjectInit, IDisposable
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private bool _initialized;

        private MetricServer? _metricServer;

        public void Initialize()
        {
            _initialized = true;

            Reload();
        }

        void IPostInjectInit.PostInject()
        {
            _configurationManager.RegisterCVar("metrics.enabled", false, onValueChanged: _ => Reload());
            _configurationManager.RegisterCVar("metrics.host", "localhost", onValueChanged: _ => Reload());
            _configurationManager.RegisterCVar("metrics.port", 44880, onValueChanged: _ => Reload());
        }

        private async void Stop()
        {
            if (_metricServer == null)
            {
                return;
            }

            Logger.InfoS("metrics", "Shutting down metrics.");
            await _metricServer.StopAsync();
            _metricServer = null;
        }

        void IDisposable.Dispose()
        {
            Stop();

            _initialized = false;
        }

        private void Reload()
        {
            if (!_initialized)
            {
                return;
            }

            Stop();

            var enabled = _configurationManager.GetCVar<bool>("metrics.enabled");
            if (!enabled)
            {
                return;
            }

            var host = _configurationManager.GetCVar<string>("metrics.host");
            var port = _configurationManager.GetCVar<int>("metrics.port");

            Logger.InfoS("metrics", "Prometheus metrics enabled, host: {1} port: {0}", port, host);
            _metricServer = new MetricServer(host, port);
            _metricServer.Start();
        }
    }

    internal interface IMetricsManager
    {
        void Initialize();
    }
}
