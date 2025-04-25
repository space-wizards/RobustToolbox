using Prometheus;
using System;

namespace Robust.Shared.Observability;

public abstract record Counter {
    public void Inc(int value = 1) {
        switch(this) {
            case PrometheusCounter counter:
                counter.Wrapped.Inc(value);

                return;
            default:
                throw new NotImplementedException();
        }
    }

    public void IncTo(long value) {
        switch(this) {
            case PrometheusCounter counter:
                counter.Wrapped.IncTo(value);

                return;
            default:
                throw new NotImplementedException();
        }
    }
}

internal record PrometheusCounter(Prometheus.Counter Wrapped) : Counter;
