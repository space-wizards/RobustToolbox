using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Robust.Shared.Utility;

namespace Robust.Server.DataMetrics;

internal sealed partial class MetricsManager : IMeterFactory
{
    private readonly Dictionary<string, List<CachedMeter>> _meterCache = new();
    private readonly object _meterCacheLock = new();

    Meter IMeterFactory.Create(MeterOptions options)
    {
        if (options.Scope != null && options.Scope != this)
            throw new InvalidOperationException("Cannot specify a custom scope when creating a meter");

        lock (_meterCacheLock)
        {
            if (LockedFindCachedMeter(options) is { } cached)
                return cached.Meter;

            var meter = new Meter(options.Name, options.Version, options.Tags, this);
            var meterList = _meterCache.GetOrNew(options.Name);
            meterList.Add(new CachedMeter(options.Version, TagsToDict(options.Tags), meter));
            return meter;
        }
    }

    private CachedMeter? LockedFindCachedMeter(MeterOptions options)
    {
        if (!_meterCache.TryGetValue(options.Name, out var metersList))
            return null;

        var tagsDict = TagsToDict(options.Tags);

        foreach (var cachedMeter in metersList)
        {
            if (cachedMeter.Version == options.Version && TagsMatch(tagsDict, cachedMeter.Tags))
                return cachedMeter;
        }

        return null;
    }

    private static bool TagsMatch(Dictionary<string, object?> a, Dictionary<string, object?> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var (key, valueA) in a)
        {
            if (!b.TryGetValue(key, out var valueB))
                return false;

            if (!Equals(valueA, valueB))
                return false;
        }

        return true;
    }

    private static Dictionary<string, object?> TagsToDict(IEnumerable<KeyValuePair<string, object?>>? tags)
    {
        return tags?.ToDictionary() ?? [];
    }

    private void DisposeMeters()
    {
        lock (_meterCacheLock)
        {
            foreach (var meters in _meterCache.Values)
            {
                foreach (var meter in meters)
                {
                    meter.Meter.Dispose();
                }
            }
        }
    }

    private sealed class CachedMeter(string? version, Dictionary<string, object?> tags, Meter meter)
    {
        public readonly string? Version = version;
        public readonly Dictionary<string, object?> Tags = tags;
        public readonly Meter Meter = meter;
    }
}
