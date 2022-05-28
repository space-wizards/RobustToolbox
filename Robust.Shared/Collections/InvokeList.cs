using System;

namespace Robust.Shared.Collections;

/// <summary>
/// Alternative to multi-cast delegates that has separate equality keys.
/// </summary>
/// <typeparam name="T">The type of delegate to store.</typeparam>
/// <remarks>
/// While this type is immutable (via copies), creating a copy (via add or remove) is not currently thread safe.
/// This is in contrast to multi-cast delegate.
/// </remarks>
internal struct InvokeList<T>
{
    private Entry[]? _entries;

    public int Count => _entries?.Length ?? 0;

    /// <summary>
    /// Add an entry to the current invoke list, mutating it.
    /// </summary>
    /// <param name="value">Actual value to store.</param>
    /// <param name="equality">Equality comparison key.</param>
    public void AddInPlace(T value, object equality)
    {
        this = Add(value, equality);
    }

    /// <summary>
    /// Add an entry to the invoke list, returning a new instance. The original list is not modified.
    /// </summary>
    /// <param name="value">Actual value to store.</param>
    /// <param name="equality">Equality comparison key.</param>
    public readonly InvokeList<T> Add(T value, object equality)
    {
        if (_entries == null)
        {
            return new InvokeList<T>
            {
                _entries = new[]
                {
                    new Entry { Value = value, Equality = equality }
                }
            };
        }

        var arr = _entries;
        Array.Resize(ref arr, arr.Length + 1);
        arr[^1] = new Entry { Value = value, Equality = equality };

        return new InvokeList<T>
        {
            _entries = arr
        };
    }

    /// <summary>
    /// Remove an entry from the current invoke list, mutating it.
    /// </summary>
    /// <param name="equality">Equality comparison key.</param>
    public void RemoveInPlace(object equality)
    {
        this = Remove(equality);
    }

    /// <summary>
    /// Remove an entry from the invoke list, returning a new instance. The original list is not modified.
    /// </summary>
    /// <param name="equality">Equality comparison key.</param>
    public readonly InvokeList<T> Remove(object equality)
    {
        if (_entries == null)
            return this;

        // Find if we even have this key in the list.
        var entryIdx = -1;
        for (var i = 0; i < _entries.Length; i++)
        {
            var entry = _entries[i];
            if (equality.Equals(entry))
            {
                entryIdx = i;
                break;
            }
        }

        // Entry not in the list, copy is identical.
        if (entryIdx < 0)
            return this;

        // Would remove the last element from the array, new instance is empty.
        if (_entries.Length == 1)
            return default;

        // Create new backing array and copy stuff into it.
        var newEntries = new Entry[_entries.Length - 1];
        for (var i = 0; i < entryIdx; i++)
        {
            newEntries[i] = _entries[i];
        }

        for (var i = entryIdx + 1; i < _entries.Length; i++)
        {
            newEntries[entryIdx - 1] = _entries[entryIdx];
        }

        return new InvokeList<T>
        {
            _entries = newEntries
        };
    }

    public ReadOnlySpan<Entry> Entries => _entries;

    public struct Entry
    {
        public T? Value;
        public object? Equality;
    }
}
