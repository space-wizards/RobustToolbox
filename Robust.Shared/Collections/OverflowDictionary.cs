using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Collections;

/// <summary>
/// A dictionary with a maximum capacity which will override the oldest inserted value when a new value is inserted.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public sealed class OverflowDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable where TKey : notnull
{
    private TKey[] _insertionQueue;
    private int _currentIndex = 0;
    private IDictionary<TKey, TValue> _dict;

    /// <summary>
    /// The maximum capacity of the dictionary.
    /// </summary>
    public int Capacity => _insertionQueue.Length;

    /// <summary>
    /// A function used to dispose of values overwritten by the overflow functionality.
    /// </summary>
    private Action<TValue>? _valueDisposer;

    /// <param name="capacity">The maximum capacity of the dictionary.</param>
    /// <param name="valueDisposer">A function used to dispose of values overwritten by the overflow functionality.</param>
    /// <exception cref="InvalidOperationException">Thrown in the <paramref name="capacity"/> is less than 1.</exception>
    public OverflowDictionary(int capacity, Action<TValue>? valueDisposer = null)
    {
        if (capacity <= 0)
        {
            throw new InvalidOperationException(
                $"Cannot create an {nameof(OverflowDictionary<TKey, TValue>)} with a capacity of less than 1.");
        }

        _valueDisposer = valueDisposer;
        _dict = new Dictionary<TKey, TValue>(capacity);
        _insertionQueue = new TKey[capacity];
    }

    private int GetArrayStartIndex()
    {
        return _currentIndex % Capacity;
    }

    public void Clear()
    {
        _dict.Clear();
        Array.Clear(_insertionQueue);
    }

    public void Add(TKey key, TValue value)
    {
        if (_dict.ContainsKey(key))
            throw new InvalidOperationException("Tried inserting duplicate key.");

        if (Count == Capacity)
        {
            var startIndex = GetArrayStartIndex();
            var entry = _insertionQueue[startIndex];
            Array.Clear(_insertionQueue, startIndex, 1);
            _valueDisposer?.Invoke(_dict[entry]);
            _dict.Remove(entry);
        }
        _dict.Add(key, value);
        _insertionQueue[_currentIndex++] = key;
        if (_currentIndex == Capacity)
        {
            _currentIndex = 0;
        }
    }

    /// <summary>
    ///     Variant of <see cref="Add(TKey, TValue)"/> that also returns any entry that was removed to make room for the new entry.
    /// </summary>
    public bool Add(TKey key, TValue value, [NotNullWhen(true)] out (TKey Key, TValue Value)? old)
    {
        if (_dict.ContainsKey(key))
            throw new InvalidOperationException("Tried inserting duplicate key.");

        if (Count == Capacity)
        {
            var startIndex = GetArrayStartIndex();
            var entry = _insertionQueue[startIndex];
            _dict.Remove(entry, out var oldValue);
            Array.Clear(_insertionQueue, startIndex, 1);
            _valueDisposer?.Invoke(oldValue!);
            old = (entry, oldValue!);
        }
        else
            old = null;

        _dict.Add(key, value);
        _insertionQueue[_currentIndex++] = key;
        if (_currentIndex == Capacity)
        {
            _currentIndex = 0;
        }

        return old != null;
    }

    public bool Remove(TKey key)
    {
        //it doesnt make sense for my usecase so i left this unimplemented. i cba to bother with moving all the entries in the array around etc.
        throw new NotImplementedException("Removing from an Overflowdictionary is not yet supported");
    }

    #region Redirects
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public bool Contains(KeyValuePair<TKey, TValue> item) => _dict.Contains(item);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => _dict.CopyTo(array, arrayIndex);
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
    public ICollection<TKey> Keys => _dict.Keys;
    public ICollection<TValue> Values => _dict.Values;
    public int Count => _dict.Count;
    public bool IsReadOnly => false;
    public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value) => _dict.TryGetValue(key, out value!);
    public TValue this[TKey key]
    {
        get => _dict[key];
        set => _dict[key] = value;
    }
    #endregion

    public void Dispose()
    {
        foreach (var value in _dict.Values)
        {
            _valueDisposer?.Invoke(value);
        }
    }
}
