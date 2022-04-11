using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Robust.Shared.Collections;

public sealed class CircularQueue<T>
{
    private readonly T[] _queue;
    private int _currentIdx = 0;
    private int _length = 0;
    public readonly int Size;

    public CircularQueue(int size)
    {
        _queue = new T[size];
        Size = size;
    }

    public void Enqueue(T item)
    {
        _queue[_currentIdx++] = item;
        if(_length < Size)
            _length++;

        if (_currentIdx == Size)
        {
            _currentIdx = 0;
        }
    }

    public T Dequeue()
    {
        if (!TryDequeue(out var item))
        {
            throw new InvalidOperationException($"{nameof(CircularQueue<T>)} has no more items to dequeue.");
        }

        return item;
    }

    public bool TryDequeue([NotNullWhen(true)] out T? item)
    {
        if (_length == 0)
        {
            item = default;
            return false;
        }

        item = _queue[GetCurrentIndex()]!;
        _length--;
        return true;
    }

    private int GetCurrentIndex()
    {
        Debug.Assert(_length > 0);
        var idx = _currentIdx - _length;
        if (idx < 0)
        {
            return idx + Size;
        }

        return idx;
    }

    public T Peek()
    {
        if (_length == 0)
        {
            throw new InvalidOperationException($"{nameof(CircularQueue<T>)} has no more items to dequeue.");
        }

        return _queue[GetCurrentIndex()];
    }

    public bool Contains(T item)
    {
        for (int i = 0; i < _length; i++)
        {
            var actualIndex = _currentIdx + i;
            if (actualIndex >= _length)
                actualIndex -= _length;
            if (EqualityComparer<T>.Default.Equals(item, _queue[actualIndex])) return true;
        }
        return false;
    }

    public T[] ToArray()
    {
        if (_length == 0)
        {
            return Array.Empty<T>();
        }

        var res = new T[_length];

        var startIdx = _currentIdx - _length;
        if (startIdx < 0)
        {
            Array.Copy(_queue, startIdx + Size, res, 0, -1 * startIdx);
            Array.Copy(_queue, 0, res, -1 * startIdx, startIdx + Size);
        }
        else
        {
            Array.Copy(_queue, startIdx, res, 0, _length);
        }

        return res;
    }
}
