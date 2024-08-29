using System;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Prometheus;
using Prometheus.DotNetRuntime;
using Prometheus.DotNetRuntime.Metrics.Producers;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using EventSource = System.Diagnostics.Tracing.EventSource;

namespace Robust.Server.DataMetrics;

/// <summary>
/// Manages OpenTelemetry metrics exposure.
/// </summary>
/// <remarks>
/// <para>
/// If enabled via <see cref="CVars.MetricsEnabled"/>, metrics about the game server are exposed via a HTTP server
/// in an OpenTelemetry-compatible format (Prometheus).
/// </para>
/// <para>
/// Metrics can be added through the types in <c>System.Diagnostics.Metrics</c> or <c>Prometheus</c>.
/// IoC contains an implementation of <see cref="IMeterFactory"/> that can be used to instantiate meters.
/// </para>
/// </remarks>
public interface IMetricsManager
{
    /// <summary>
    /// An event that gets raised on the main thread when complex metrics should be updated.
    /// </summary>
    /// <remarks>
    /// This event is raised on the main thread before a Prometheus collection happens,
    /// and also with a fixed interval if <see cref="CVars.MetricsUpdateInterval"/> is set.
    /// You can use it to update complex metrics that can't "just" be stuffed into a counter.
    /// </remarks>
    event Action UpdateMetrics;
}

internal sealed partial class MetricsManager : IMetricsManagerInternal, IDisposable
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    private bool _initialized;

    private ManagedHttpListenerMetricsServer? _metricServer;
    private IDisposable? _runtimeCollector;
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("metrics");

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

        InitializeUpdateMetrics();
    }

    private async Task Stop()
    {
        if (_metricServer == null)
        {
            return;
        }

        _sawmill.Info("Shutting down metrics.");
        await _metricServer.StopAsync();
        _metricServer = null;
        _runtimeCollector?.Dispose();
        _runtimeCollector = null;
    }

    async void IDisposable.Dispose()
    {
        DisposeMeters();

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

        _sawmill.Info("Prometheus metrics enabled, host: {1} port: {0}", port, host);
        var sawmill = Logger.GetSawmill("metrics.server");
        _metricServer = new ManagedHttpListenerMetricsServer(
            sawmill,
            host,
            port,
            registry: Metrics.DefaultRegistry,
            beforeCollect: BeforeCollectCallback);
        _metricServer.Start();

        if (_cfg.GetCVar(CVars.MetricsRuntime))
        {
            _sawmill.Debug("Enabling runtime metrics");
            _runtimeCollector = BuildRuntimeStats().StartCollecting();
        }
    }

    private DotNetRuntimeStatsBuilder.Builder BuildRuntimeStats()
    {
        var builder = DotNetRuntimeStatsBuilder.Customize();

        if (CapLevel(CVars.MetricsRuntimeGc) is { } gc)
        {
            var buckets = Buckets(CVars.MetricsRuntimeGcHistogram, 1000);
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
        double[] Buckets(CVarDef<string> cvar, double divide=1)
        {
            return _cfg.GetCVar(cvar)
                .Split(',')
                .Select(x => double.Parse(x, CultureInfo.InvariantCulture) / divide)
                .ToArray();
        }

        return builder;
    }

    [EventSource(Name = "Robust.MetricsManager")]
    private sealed class MetricsEvents : EventSource
    {
        public static MetricsEvents Log { get; } = new();

        [Event(1)]
        public void ScrapeStart() => WriteEvent(1);

        [Event(2)]
        public void ScrapeStop() => WriteEvent(2);

        [Event(3)]
        public void RequestStart() => WriteEvent(3);

        [Event(4)]
        public void RequestStop() => WriteEvent(4);
    }
}

internal interface IMetricsManagerInternal : IMetricsManager
{
    void Initialize();
    void FrameUpdate();
}
