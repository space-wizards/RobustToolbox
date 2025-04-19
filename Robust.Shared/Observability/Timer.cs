using Prometheus;
using System;

namespace Robust.Shared.Observability;

/// <summary>
/// A timer that is used to observe a duration of elapsed time.
/// </summary>
public abstract record Timer : IDisposable
{
    public void Dispose()
    {
        switch(this) {
            case PrometheusTimer timer:
                timer.Wrapped.Dispose();
                // Doing this because the linter told me so :godo:
                // TODO: Confirm what to do about this w/ C# experts.
                GC.SuppressFinalize(this);

                return;
            default:
                throw new NotImplementedException();
        }
    }

    public TimeSpan ObserveDuration() {
        return this switch
        {
            PrometheusTimer timer => timer.Wrapped.ObserveDuration(),
            _ => throw new NotImplementedException(),
        };
    }
}

internal record PrometheusTimer(ITimer Wrapped) : Timer;
