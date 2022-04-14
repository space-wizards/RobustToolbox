
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;

namespace Robust.Shared.Collections;

public sealed class OverflowDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable where TKey : notnull
{
    private TKey[] _insertionQueue;
    private int _currentIndex = 0;
    private IDictionary<TKey, TValue> _dict;
    public int Capacity => _insertionQueue.Length;
    private Action<TValue>? _valueDisposer;

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
        var idx = _currentIndex - Count;
        if (idx < 0)
            idx += Capacity;
        return idx;
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
