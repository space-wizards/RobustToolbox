using Prometheus;
using System;

namespace Robust.Shared.Observability;

// Wrapper so we can hide Prom dep
public abstract record Gauge
{
    public void Set(double val) {
        switch(this) {
            case PrometheusGauge gauge:
                gauge.Wrapped.Set(val);

                return;
            default:
                throw new NotImplementedException();
        }
    }
}

internal record PrometheusGauge(Prometheus.Gauge Wrapped) : Gauge;
