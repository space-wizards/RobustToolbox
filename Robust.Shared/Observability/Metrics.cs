using System;
using Prometheus;

namespace Robust.Shared.Observability;

// Stand-in for the Prometheus Metrics class.
public static class Metrics {
    public static Gauge Gauge(string name, string help) {
        return new PrometheusGauge(Prometheus.Metrics.CreateGauge(name, help));
    }

    public static Histogram Histogram(
        string name,
        string help,
        string? labelName,
        double bucketsStart,
        double bucketsFactor,
        int bucketsCount
    ) {
        var conf = new HistogramConfiguration
        {
            Buckets = Prometheus.Histogram.ExponentialBuckets(bucketsStart, bucketsFactor, bucketsCount)
        };

        if (labelName != null) {
            conf.LabelNames = [labelName];
        }

        return new PrometheusParent(Prometheus.Metrics.CreateHistogram(name, help, conf));
    }

    public static Counter Counter(string name, string help) {
        return new PrometheusCounter(Prometheus.Metrics.CreateCounter(name, help));
    }
}
