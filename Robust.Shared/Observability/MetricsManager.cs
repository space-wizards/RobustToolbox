using System;

namespace Robust.Shared.Observability;

/// <summary>
/// Manages metrics exposure.
/// </summary>
/// <remarks>
/// <para>
/// If enabled via <see cref="CVars.MetricsEnabled"/>, metrics about the game server are exposed via a HTTP server
/// in an OpenTelemetry-compatible format.
/// </para>
/// <para>
/// Metrics can be added through the types in <c>System.Diagnostics.Metrics</c> or <c>Robust.Shared.Observability</c>.
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

internal interface IMetricsManagerInternal : IMetricsManager
{
    void Initialize();
    void FrameUpdate();
}
