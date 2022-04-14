using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Robust.Shared.Collections;

/// <summary>
/// A fixed-size queue that discards the oldest entry if a new entry is enqueued when the queue is full.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class OverflowQueue<T>
{
    private readonly T[] _queue;
    private int _currentIdx = 0;
    private int _length = 0;
    /// <summary>
    /// The size of the queue-buffer.
    /// </summary>
    public int Size => _queue.Length;

    /// <param name="size">size of the queue-buffer</param>
    public OverflowQueue(int size)
    {
        _queue = new T[size];
    }

    /// <summary>
    /// Enqueues the <paramref name="item"/>. Overrides the oldest item if the queue is full.
    /// </summary>
    /// <param name="item">The item to enqueue</param>
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

    /// <summary>
    /// Removes the item at the head of the queue and returns it. If the queue is empty, this method throws an InvalidOperationException.
    /// </summary>
    /// <returns>The dequeued item.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the queue is empty.</exception>
    public T Dequeue()
    {
        if (!TryDequeue(out var item))
        {
            throw new InvalidOperationException($"{nameof(OverflowQueue<T>)} has no more items to dequeue.");
        }

        return item;
    }

    /// <summary>
    /// Tries to dequeue an item.
    /// </summary>
    /// <param name="item">The item which got dequeued. Null if the queue was empty</param>
    /// <returns>True if an item was dequeued, false if not.</returns>
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

    /// <summary>
    /// Returns the item at the head of the queue. The object remains in the queue. If the queue is empty, this method throws an InvalidOperationException.
    /// </summary>
    /// <returns>The item at the head of the queue.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the queue is empty.</exception>
    public T Peek()
    {
        if (_length == 0)
        {
            throw new InvalidOperationException($"{nameof(OverflowQueue<T>)} has no more items to dequeue.");
        }

        return _queue[GetCurrentIndex()];
    }

    /// <summary>
    /// Returns true if the queue contains at least one object equal to item. Equality is determined using EqualityComparer<T>.Default.Equals().
    /// </summary>
    /// <param name="item">Item to look for.</param>
    /// <returns>True if the queue contains the item, false if not.</returns>
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

    /// <summary>
    /// Returns the queue contents first to last as an array.
    /// </summary>
    /// <returns>The array containing the queue contents.</returns>
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
