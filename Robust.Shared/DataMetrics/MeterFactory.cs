using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Shared.DataMetrics;

internal interface IMeterFactoryInternal : IMeterFactory
{
    void Initialize();
}

[Virtual]
internal class MeterFactory : IMeterFactoryInternal
{
    [Dependency] protected readonly IConfigurationManager Cfg = null!;

    private readonly Dictionary<string, List<CachedMeter>> _meterCache = new();
    private readonly Lock _meterCacheLock = new();

    private string? _instanceName;

    public virtual void Initialize()
    {
        _instanceName = Cfg.GetCVar(CVars.MetricsInstanceName);
        if (string.IsNullOrEmpty(_instanceName))
            _instanceName = null;
    }

    Meter IMeterFactory.Create(MeterOptions options)
    {
        if (options.Scope != null && options.Scope != this)
            throw new InvalidOperationException("Cannot specify a custom scope when creating a meter");

        lock (_meterCacheLock)
        {
            if (LockedFindCachedMeter(options) is { } cached)
                return cached.Meter;

            var tags = options.Tags;
            if (_instanceName != null)
            {
                tags =
                [
                    ..options.Tags ?? [],
                    new KeyValuePair<string, object?>("instance", _instanceName)
                ];
            }

            // ReSharper disable once PossibleMultipleEnumeration
            var meter = new Meter(options.Name, options.Version, tags, this);
            var meterList = _meterCache.GetOrNew(options.Name);
            // ReSharper disable once PossibleMultipleEnumeration
            meterList.Add(new CachedMeter(options.Version, TagsToDict(tags), meter));
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

    void IDisposable.Dispose()
    {
        Dispose();
    }

    protected virtual void Dispose()
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
