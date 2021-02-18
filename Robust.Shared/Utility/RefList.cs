using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Robust.Shared.Utility
{
    /// <summary>
    ///     Supposedly a high-performance version of <see cref="List{T}"/>,
    ///     that allows fetching direct references to the underlying contents.
    /// </summary>
    /// <remarks>
    ///     Due to this type's nature,
    ///     keeping references to the contents of this list while mutating it is undefined behavior.
    ///     Don't do it.
    /// </remarks>
    /// <typeparam name="T">The type of the contents of the list. This must be an unmanaged type.</typeparam>
    public class RefList<T> : IList<T>
    {
        private T[] _array;
        private int _size;

        public RefList() : this(1)
        {
        }

        public RefList(int initialCapacity)
        {
            _array = new T[initialCapacity];
            _size = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Allocate a new member in the list and return the reference to it for initialization.
        /// </summary>
        /// <returns></returns>
        public ref T AllocAdd()
        {
            _ensureCapacity(_size+1);

            return ref _array[_size++];
        }

        /// <summary>
        ///     It is probably advisable to use <see cref="AllocAdd"/> instead for better performance.
        /// </summary>
        public void Add(T item)
        {
            _ensureCapacity(_size+1);

            _array[_size++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_array, 0, _size);
            }
            _size = 0;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_array, 0, array, arrayIndex, _size);
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index == -1)
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        public int Count => _size;
        public bool IsReadOnly => false;
        public int IndexOf(T item)
        {
            return Array.IndexOf(_array, item, 0, _size);
        }

        public void Insert(int index, T item)
        {
            _ensureCapacity(_size+1);

            if (index < _size)
            {
                Array.Copy(_array, index, _array, index+1, _size - index);
            }

            _array[index] = item;
            _size++;
        }

        public void RemoveAt(int index)
        {
            if (index >= _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must fit into list.");
            }

            _size -= 1;
            // No need to do a copy if the last element gets removed.
            if (index < _size)
            {
                Array.Copy(_array, index + 1, _array, index, _size - index);
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _array[_size] = default!;
            }
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[index];
        }

        T IList<T>.this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }

        public void Sort(IComparer<T> comparer)
        {
            Array.Sort(_array, 0, _size, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> GetSpan()
        {
            return new(_array, 0, _size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _ensureCapacity(int newCapacity)
        {
            if (newCapacity < _array.Length)
            {
                return;
            }

            var old = _array;
            _array = new T[old.Length * 2];
            Array.Copy(old, 0, _array, 0, _size);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly RefList<T> _owner;
            private int _position;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(RefList<T> owner)
            {
                _owner = owner;
                _position = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return ++_position < _owner._size;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                _position = 0;
            }

            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _owner._array[_position];
            }

            T IEnumerator<T>.Current => Current;

            object? IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                // Nada, at least nothing yet.
            }
        }
    }
}
