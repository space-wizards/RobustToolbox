using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Robust.Shared.Collections;

/// <summary>
/// A bounded, clearable string sharer. Unlike <see cref="string.Intern(string)"/>, entries are owned by
/// this instance and can be released with <see cref="Clear"/>.
/// </summary>
public sealed class StringInterner
{
    private readonly ConcurrentDictionary<string, string> _entries;
    private readonly int _maximumEntries;
    private readonly int _maximumLength;

    public StringInterner(int maximumEntries, int maximumLength = int.MaxValue, IEqualityComparer<string>? comparer = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumEntries, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumLength, 0);

        _maximumEntries = maximumEntries;
        _maximumLength = maximumLength;
        _entries = new ConcurrentDictionary<string, string>(comparer ?? StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the instance for <paramref name="value"/>.
    /// </summary>
    public string Intern(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length > _maximumLength)
            return value;

        if (_entries.TryGetValue(value, out var existing))
            return existing;

        // The bound is intentionally best-effort when several threads add distinct strings concurrently.
        if (_entries.Count >= _maximumEntries)
            return value;

        return _entries.GetOrAdd(value, value);
    }

    public void Clear()
    {
        _entries.Clear();
    }
}
