using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Prometheus;
using Prometheus.DotNetRuntime;
using Prometheus.DotNetRuntime.Metrics.Producers;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

#nullable enable

namespace Robust.Server.DataMetrics
{
    internal sealed class MetricsManager : IMetricsManager, IDisposable
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        private bool _initialized;

        private MetricServer? _metricServer;
        private IDisposable? _runtimeCollector;

        public void Initialize()
        {
            _initialized = true;

            ValueChanged(CVars.MetricsEnabled);
            ValueChanged(CVars.MetricsHost);
            ValueChanged(CVars.MetricsPort);
            ValueChanged(CVars.MetricsRuntime);
            ValueChanged(CVars.MetricsRuntimeGc);
            ValueChanged(CVars.MetricsRuntimeGcHistogram);
            ValueChanged(CVars.MetricsRuntimeContention);
            ValueChanged(CVars.MetricsRuntimeContentionSampleRate);
            ValueChanged(CVars.MetricsRuntimeThreadPool);
            ValueChanged(CVars.MetricsRuntimeThreadPoolQueueHistogram);
            ValueChanged(CVars.MetricsRuntimeJit);
            ValueChanged(CVars.MetricsRuntimeJitSampleRate);
            ValueChanged(CVars.MetricsRuntimeException);
            ValueChanged(CVars.MetricsRuntimeSocket);

            Reload();

            void ValueChanged<T>(CVarDef<T> cVar) where T : notnull
            {
                _cfg.OnValueChanged(cVar, _ => Reload());
            }
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
            _runtimeCollector?.Dispose();
            _runtimeCollector = null;
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

            var enabled = _cfg.GetCVar(CVars.MetricsEnabled);
            _entitySystemManager.MetricsEnabled = enabled;

            if (!enabled)
            {
                return;
            }

            var host = _cfg.GetCVar(CVars.MetricsHost);
            var port = _cfg.GetCVar(CVars.MetricsPort);

            Logger.InfoS("metrics", "Prometheus metrics enabled, host: {1} port: {0}", port, host);
            _metricServer = new MetricServer(host, port);
            _metricServer.Start();

            if (_cfg.GetCVar(CVars.MetricsRuntime))
            {
                Logger.DebugS("metrics", "Enabling runtime metrics");
                _runtimeCollector = BuildRuntimeStats().StartCollecting();
            }
        }

        private DotNetRuntimeStatsBuilder.Builder BuildRuntimeStats()
        {
            var builder = DotNetRuntimeStatsBuilder.Customize();

            if (CapLevel(CVars.MetricsRuntimeGc) is { } gc)
            {
                var buckets = Buckets(CVars.MetricsRuntimeGcHistogram);
                builder.WithGcStats(gc, buckets);
            }

            if (CapLevel(CVars.MetricsRuntimeContention) is { } contention)
            {
                var rate = _cfg.GetCVar(CVars.MetricsRuntimeContentionSampleRate);
                builder.WithContentionStats(contention, (SampleEvery)rate);
            }

            if (CapLevel(CVars.MetricsRuntimeThreadPool) is { } threadPool)
            {
                builder.WithThreadPoolStats(threadPool, new ThreadPoolMetricsProducer.Options
                {
                    QueueLengthHistogramBuckets = Buckets(CVars.MetricsRuntimeThreadPoolQueueHistogram)
                });
            }

            if (CapLevel(CVars.MetricsRuntimeJit) is { } jit)
            {
                var rate = _cfg.GetCVar(CVars.MetricsRuntimeJitSampleRate);
                builder.WithJitStats(jit, (SampleEvery) rate);
            }

            if (CapLevel(CVars.MetricsRuntimeException) is { } exception)
            {
                builder.WithExceptionStats(exception);
            }

            if (CapLevel(CVars.MetricsRuntimeSocket) is { })
            {
                builder.WithSocketStats();
            }

            CaptureLevel? CapLevel(CVarDef<string> cvar)
            {
                var val = _cfg.GetCVar(cvar);
                if (val != "")
                    return Enum.Parse<CaptureLevel>(val);

                return null;
            }

            // 🪣
            double[] Buckets(CVarDef<string> cvar)
            {
                return _cfg.GetCVar(cvar)
                    .Split()
                    .Select(x => double.Parse(x, CultureInfo.InvariantCulture) / 1000)
                    .ToArray();
            }

            return builder;
        }
    }

    internal interface IMetricsManager
    {
        void Initialize();
    }
}
