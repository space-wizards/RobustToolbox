using System;
using System.Threading.Tasks;
using Prometheus;
using Robust.Shared;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

#nullable enable

namespace Robust.Server.DataMetrics
{
    internal sealed class MetricsManager : IMetricsManager, IDisposable
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        private bool _initialized;

        private MetricServer? _metricServer;

        public void Initialize()
        {
            _initialized = true;

            _configurationManager.OnValueChanged(CVars.MetricsEnabled, _ => Reload());
            _configurationManager.OnValueChanged(CVars.MetricsHost, _ => Reload());
            _configurationManager.OnValueChanged(CVars.MetricsPort, _ => Reload());

            Reload();
        }

        private async Task Stop()
        {
            if (_metricServer == null)
            {
                return;
            }

            Logger.InfoS("metrics", "Shutting down metrics.");
            await _metricServer.StopAsync();
            _metricServer = null;
        }

        async void IDisposable.Dispose()
        {
            await Stop();

            _initialized = false;
        }

        private async void Reload()
        {
            if (!_initialized)
            {
                return;
            }

            await Stop();

            var enabled = _configurationManager.GetCVar(CVars.MetricsEnabled);
            _entitySystemManager.MetricsEnabled = enabled;

            if (!enabled)
            {
                return;
            }

            var host = _configurationManager.GetCVar(CVars.MetricsHost);
            var port = _configurationManager.GetCVar(CVars.MetricsPort);

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
