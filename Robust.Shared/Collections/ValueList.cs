// This file includes code based on the List<T> class from https://github.com/dotnet/runtime/
// The original code is Copyright © .NET Foundation and Contributors. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Robust.Shared.Collections;

/// <summary>
/// Implementation of <see cref="List{T}"/> that is stored in a struct instead.
/// </summary>
/// <remarks>
/// <para>
/// Storing this implementation in a struct reduces GC and memory overhead from list instances drastically.
/// It is only recommended you use this class for private data;
/// public APIs probably shouldn't expose it unless you know what you're doing.
/// </para>
/// <para>
/// This implementation does not complain if you modify it during iteration. Be careful!
/// </para>
/// <para>
/// The implementation uses an array to store the contained items.
/// This array may be larger (<see cref="Capacity"/>) than the amount of "actual" items stored (<see cref="Count"/>).
/// Adding or removing elements to the list shrinks or grows the available capacity at the end of the array.
/// If there is no remaining capacity left when inserting,
/// a new, larger, array is allocated and elements are copied over.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of item to store in the list.</typeparam>
public struct ValueList<T> : IEnumerable<T>
{
    private const int DefaultCapacity = 4;

    // List can be null so that the list instance is valid if = defaulted.
    // Null backing list is equal to empty array everywhere.
    // It follows from this that having a count or capacity > 0 means the list is not null.
    private T[]? _items;

    // Constructs a List with a given initial capacity. The list is
    // initially empty, but will have room for the given number of elements
    // before any reallocations are required.
    //
    public ValueList(int capacity)
    {
        _items = capacity == 0 ? null : new T[capacity];
        Count = 0;
    }

    /// <summary>
    /// Create a list by copying the contents from another enumerable.
    /// </summary>
    /// <param name="collection">The enumerable to copy the items from.</param>
    public ValueList(IEnumerable<T> collection)
    {
        _items = collection.ToArray();
        Count = _items.Length;
    }

    /// <summary>
    /// Create a list by taking ownership of an existing array.
    /// Mutations of the list may mutate the passed array.
    /// The count and capacity of the list are both set equal to the array length.
    /// </summary>
    /// <remarks>
    /// If null is passed, it is treated equivalently to an empty array.
    /// </remarks>
    public static ValueList<T> OwningArray(T[]? array)
    {
        ValueList<T> list = default;
        list._items = array;
        list.Count = list.Capacity;
        return list;
    }

    /// <summary>
    /// Create a list by taking ownership of an existing array.
    /// Mutations of the list may mutate the passed array.
    /// The capacity is set to the length of the list.
    /// The count can be set separately if the array has more space than valid items.
    /// </summary>
    /// <remarks>
    /// If null is passed, it is treated equivalently to an empty array.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown if count is negative or if count is greater than the array capacity.
    /// </exception>
    public static ValueList<T> OwningArray(T[]? array, int count)
    {
        ValueList<T> list = default;
        list._items = array;

        if (count < 0)
            throw new ArgumentException("Count cannot be negative.");

        if (count >= list.Capacity)
            throw new ArgumentException("Count cannot be greater than the size of the array.");

        list.Count = count;
        return list;
    }

    public int Count { get; private set; }

    // Sets or Gets the element at the given index.
    public readonly ref T this[int index]
    {
        get
        {
            // Following trick can reduce the range check by one
            if ((uint)index >= (uint)Count)
                throw new IndexOutOfRangeException();

            return ref _items![index];
        }
    }

    public int Capacity
    {
        readonly get => _items?.Length ?? 0;
        set
        {
            if (value < Count)
                throw new ArgumentException("Cannot set capacity lower than contained count");

            if (value == Capacity)
                return;

            if (value > 0)
            {
                var newItems = new T[value];
                if (Count > 0)
                    Array.Copy(_items!, newItems, Count);

                _items = newItems;
            }
            else
            {
                _items = null;
            }
        }
    }

    /// <summary>
    /// Span containing the items inside the list.
    /// Note that resizing of the backing array will cause this span to be invalidated.
    /// </summary>
    public readonly Span<T> Span => new(_items, 0, Count);

    // Adds the given object to the end of this list. The size of the list is
    // increased by one. If required, the capacity of the list is doubled
    // before adding the new element.
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        var array = _items;
        var size = Count;
        if ((uint)size < (uint)Capacity)
        {
            Count = size + 1;
            array![size] = item;
        }
        else
        {
            AddWithResize(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T AddRef()
    {
        var array = _items;
        var size = Count;
        if ((uint)size < (uint)Capacity)
        {
            Count = size + 1;
            return ref array![size];
        }

        return ref AddRefWithResize();
    }

    // Non-inline from List.Add to improve its code quality as uncommon path
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddWithResize(T item)
    {
        Debug.Assert(Count == Capacity);

        var size = Count;
        Grow(size + 1);
        Count = size + 1;
        _items![size] = item;
    }

    // Non-inline from List.Add to improve its code quality as uncommon path
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T AddRefWithResize()
    {
        Debug.Assert(Count == Capacity);

        var size = Count;
        Grow(size + 1);
        Count = size + 1;
        return ref _items![size];
    }

    // Clears the contents of List.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var size = Count;
            Count = 0;
            if (size > 0)
            {
                Array.Clear(_items!, 0, size); // Clear the elements so that the gc can reclaim the references.
            }
        }
        else
        {
            Count = 0;
        }
    }

    // Contains returns true if the specified element is in the List.
    // It does a linear, O(n) search.  Equality is determined by calling
    // EqualityComparer<T>.Default.Equals().
    //
    public readonly bool Contains(T item)
    {
        return IndexOf(item) >= 0;
    }

    /// <summary>
    /// Ensures that the capacity of this list is at least the specified <paramref name="capacity"/>.
    /// If the current capacity of the list is less than specified <paramref name="capacity"/>,
    /// the capacity is increased by continuously twice current capacity until it is at least the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    /// <returns>The new capacity of this list.</returns>
    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
            throw new ArgumentException("Capacity cannot be negative");

        if (Capacity < capacity)
            Grow(capacity);

        return _items!.Length;
    }

    /// <summary>
    /// Increase the capacity of this list to at least the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    private void Grow(int capacity)
    {
        Debug.Assert(Capacity < capacity);

        int newcapacity = Capacity == 0 ? DefaultCapacity : 2 * _items!.Length;

        // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint)newcapacity > Array.MaxLength) newcapacity = Array.MaxLength;

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newcapacity < capacity) newcapacity = capacity;

        Capacity = newcapacity;
    }

    // Returns an enumerator for this list with the given
    // permission for removal of elements. If modifications made to the list
    // while an enumeration is in progress, the MoveNext and
    // GetObject methods of the enumerator will throw an exception.
    //
    public readonly Enumerator GetEnumerator()
        => new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this);


    // Returns the index of the first occurrence of a given value in a range of
    // this list. The list is searched forwards from beginning to end.
    // The elements of the list are compared to the given value using the
    // Object.Equals method.
    //
    // This method uses the Array.IndexOf method to perform the
    // search.
    //
    public readonly int IndexOf(T item)
        => _items == null ? -1 : Array.IndexOf(_items, item, 0, Count);

    // Returns the index of the first occurrence of a given value in a range of
    // this list. The list is searched forwards, starting at index
    // index and ending at count number of elements. The
    // elements of the list are compared to the given value using the
    // Object.Equals method.
    //
    // This method uses the Array.IndexOf method to perform the
    // search.
    //
    public readonly int IndexOf(T item, int index)
    {
        if (index > Count)
            throw new ArgumentOutOfRangeException();

        return _items == null ? -1 : Array.IndexOf(_items, item, index, Count - index);
    }

    // Returns the index of the first occurrence of a given value in a range of
    // this list. The list is searched forwards, starting at index
    // index and upto count number of elements. The
    // elements of the list are compared to the given value using the
    // Object.Equals method.
    //
    // This method uses the Array.IndexOf method to perform the
    // search.
    //
    public readonly int IndexOf(T item, int index, int count)
    {
        if (index > Count)
            throw new ArgumentException("Start index out of bounds");

        if (count < 0 || index > Count - count)
            throw new ArgumentException("Count out of range");

        return _items == null ? -1 : Array.IndexOf(_items, item, index, count);
    }

    // Inserts an element into this list at a given index. The size of the list
    // is increased by one. If required, the capacity of the list is doubled
    // before inserting the new element.
    //
    public void Insert(int index, T item)
    {
        // Note that insertions at the end are legal.
        if ((uint)index > (uint)Count)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (Count == _items!.Length) Grow(Count + 1);
        if (index < Count)
        {
            Array.Copy(_items, index, _items, index + 1, Count - index);
        }

        _items[index] = item;
        Count++;
    }

    // Returns the index of the last occurrence of a given value in a range of
    // this list. The list is searched backwards, starting at the end
    // and ending at the first element in the list. The elements of the list
    // are compared to the given value using the Object.Equals method.
    //
    // This method uses the Array.LastIndexOf method to perform the
    // search.
    //
    public readonly int LastIndexOf(T item)
    {
        if (Count == 0)
        {
            // Special case for empty list
            return -1;
        }

        return LastIndexOf(item, Count - 1, Count);
    }

    // Returns the index of the last occurrence of a given value in a range of
    // this list. The list is searched backwards, starting at index
    // index and ending at the first element in the list. The
    // elements of the list are compared to the given value using the
    // Object.Equals method.
    //
    // This method uses the Array.LastIndexOf method to perform the
    // search.
    //
    public readonly int LastIndexOf(T item, int index)
    {
        if (index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");

        return LastIndexOf(item, index, index + 1);
    }

    // Returns the index of the last occurrence of a given value in a range of
    // this list. The list is searched backwards, starting at index
    // index and upto count elements. The elements of
    // the list are compared to the given value using the Object.Equals
    // method.
    //
    // This method uses the Array.LastIndexOf method to perform the
    // search.
    //
    public readonly int LastIndexOf(T item, int index, int count)
    {
        if (Count == 0)
        {
            // Special case for empty list
            return -1;
        }

        if (index < 0)
            throw new ArgumentException("Index cannot be negative");

        if (count < 0)
            throw new ArgumentException("Count cannot be negative");

        if (index >= Count)
            throw new ArgumentException("Range outside of collection bounds");

        if (count > index + 1)
            throw new ArgumentException("Range outside of collection bounds");

        return Array.LastIndexOf(_items!, item, index, count);
    }

    // Removes the element at the given index. The size of the list is
    // decreased by one.
    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    // Removes the element at the given index. The size of the list is
    // decreased by one.
    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        Count--;
        if (index < Count)
            Array.Copy(_items!, index + 1, _items!, index, Count - index);

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            _items![Count] = default!;
    }

    public void Sort() => Span.Sort();
    public void Sort(IComparer<T>? comparer) => Span.Sort(comparer);
    public void Sort(Comparison<T> comparison) => Span.Sort(comparison);

    public readonly T[] ToArray() => Span.ToArray();

    // Sets the capacity of this list to the size of the list. This method can
    // be used to minimize a list's memory overhead once it is known that no
    // new elements will be added to the list. To completely clear a list and
    // release all memory referenced by the list, execute the following
    // statements:
    //
    // list.Clear();
    // list.TrimExcess();
    //
    public void TrimExcess()
    {
        var threshold = (int)(Capacity * 0.9);

        if (Count < threshold)
            Capacity = Count;
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly ValueList<T> _list;
        private int _index;

        internal Enumerator(ValueList<T> list)
        {
            _index = -1;
            _list = list;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            return ++_index < _list.Count;
        }

        public T Current => RefCurrent;
        public ref T RefCurrent => ref _list._items![_index];

        object? IEnumerator.Current => Current;

        void IEnumerator.Reset()
        {
            _index = -1;
        }
    }
}
