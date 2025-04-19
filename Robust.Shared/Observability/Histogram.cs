using Prometheus;
using System;

namespace Robust.Shared.Observability;

public abstract record Histogram {
    public void Observe(double val) {
        switch(this) {
            case PrometheusParent histogram:
                histogram.Wrapped.Observe(val);

                return;
            case PrometheusChild histogram:
                histogram.Wrapped.Observe(val);

                return;
        }
    }

    public Histogram Child(string name) {
        return this switch
        {
            PrometheusParent histogram => new PrometheusChild(histogram.Wrapped.WithLabels(name)),
            // TODO: This is sloppy; this should be an error if you do child-of-child that says so.
            _ => throw new NotImplementedException(),
        };
    }

    public Timer Timer(string label = "") {
        return this switch
        {
            PrometheusParent parent => new PrometheusTimer(parent.Wrapped.WithLabels(label).NewTimer()),
            PrometheusChild child => new PrometheusTimer(child.Wrapped.NewTimer()),
            _ => throw new NotImplementedException(),
        };
    }
}

internal record PrometheusParent(Prometheus.Histogram Wrapped) : Histogram;

internal record PrometheusChild(Prometheus.Histogram.Child Wrapped) : Histogram;
