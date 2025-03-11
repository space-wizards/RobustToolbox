using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Shared.Collections;

/// <summary>
/// An implementation of "generational id storage".
/// </summary>
/// <typeparam name="T">The type of value stored in this collection.</typeparam>
public sealed class GenIdStorage<T> : IGenIdStorage<GenIdStorage<T>.Key, T> {
    // The advantage of this storage method is extremely fast, O(1) lookup (way faster than Dictionary).
    // Resolving a value in the storage is a single array load and generation compare. Extremely fast.
    //
    // Disadvantages are that storage cannot be shrunk, and sparse storage is inefficient space wise.
    // Also this implementation does not have optimizations necessary to make sparse iteration efficient.
    //
    // The idea here is that the index type (NodeId in this case) has both an index and a generation.
    // The index is an integer index into the storage array, the generation is used to avoid use-after-free.
    //
    // Empty slots in the array form a linked list of free slots.
    // When we allocate a new slot, we pop one link off this linked list and hand out its index + generation.
    //
    // When we free a node, we bump the generation of the slot and make it the head of the linked list.
    // The generation being bumped means that any IDs to this slot will fail to resolve (generation mismatch).
    // The generation eventually wraps around, but if we've gone through 2^32 values in a single slot any old keys have probably been dropped.

    /// <summary>
    /// The type of key used by this collection.
    /// </summary>
    /// <param name="Index">The index of the storage slot this key is associated with.</param>
    /// <param name="Version">The version/generation of the storage slot this key is associated with.</param>
    public readonly record struct Key(int Index, int Version)
    {
        /// <summary>
        /// A key that will never be produced by a <see cref="GenIdStorage{T}"/>
        /// </summary>
        /// <remarks>
        /// A negative index and the maximum possible version number should prevent this from colliding with any naturally produced keys.
        /// </remarks>
        public readonly static Key Invalid = new(-1, -1);
    }

    /// <summary>
    /// The container for values in this storage.
    /// Includes versioning information to prevent collisions.
    /// </summary>
    /// <remarks>
    /// You could save 4 bytes/slot by turning this into a tagged union.
    /// Unfortunately, C# does not like having reference types and value types overlapping in memory.
    /// </remarks>
    internal struct Slot
    {
        /// <summary>
        /// The 'generation' of this slot.
        /// Incremented every time the slot is allocated or freed.
        /// </summary>
        public int Version;

        /// <summary>
        /// The index of the next empty slot in the storage array.
        /// May be <see cref="SENTINEL"/> if this is the last free slot.
        /// </summary>
        public int NextFree;

        /// <summary>
        /// The value currently stored in this slot.
        /// For empty slots this is the default value, possibly null.
        /// </summary>
        public T Value;

        /// <summary>
        /// True if this slot currently contains a value, false otherwise.
        /// </summary>
        public readonly bool IsOccupied
        {
            [Pure]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Version & 1) != 0;
        }
    }

    public int Count { get; private set; }

    public int Capacity => _slots.Length;

    /// <summary>
    /// The slots containing the values contained in this container.
    /// </summary>
    private Slot[] _slots;

    /// <summary>
    /// The index of the first empty slot in the storage.
    /// If this is equal to <see cref="SENTINEL"/> there are no empty slots and more must be allocated to insert a value.
    /// </summary>
    private int _nextFree = SENTINEL;

    /// <summary>
    /// An invalid index used to indicate that there are no more empty slots.
    /// </summary>
    private const int SENTINEL = -1;

    public GenIdStorage()
    {
        Count = 0;
        _slots = [];
        _nextFree = SENTINEL;
    }

    public GenIdStorage(int capacity) : this()
    {
        if (capacity > 0)
            ReAllocateTo(capacity);
    }

#region IGenIdStorage<T> impls

    /// <inheritdoc cref="IGenIdStorage{K, T}.Allocate(out K)"/>
    public ref T Allocate(out Key key)
    {
        if (_nextFree == SENTINEL)
        {
            DebugTools.Assert(Count == Capacity, "");
            ReAllocate();
        }

        var index = _nextFree;

        ref var slot = ref _slots[index];
        DebugTools.Assert(!slot.IsOccupied, "attempted to allocate an occupied slot");

        _nextFree = slot.NextFree;
        slot.Version += 1;
        Count += 1;

        key = new Key(index, slot.Version);
        return ref slot.Value;
    }

    /// <inheritdoc cref="IGenIdStorage{K, T}.Free(in K)"/>
    public void Free(in Key key)
    {
        ref var slot = ref _slots[key.Index];

        if (slot.Version != key.Version)
            throw new KeyNotFoundException($"key version {key.Version} did not match slot version {slot.Version}");

        if (!slot.IsOccupied)
            throw new InvalidOperationException("attempted to free an empty slot");

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            slot.Value = default!;

        slot.NextFree = _nextFree;
        slot.Version += 1;

        _nextFree = key.Index;
        Count -= 1;
    }

    /// <inheritdoc cref="IGenIdStorage{K, T}.this[in K]"/>
    public ref T this[in Key key]
    {
        get
        {
            ref var slot = ref _slots[key.Index];

            if (slot.Version != key.Version)
                throw new KeyNotFoundException($"key version {key.Version} did not match slot version {slot.Version}");

            return ref slot.Value;
        }
    }

    /// <inheritdoc cref="IGenIdStorage{K, T}.Clear()"/>
    public void Clear()
    {
        var capacity = _slots.Length;
        if (capacity == 0)
            return;

        for (var i = 0; i < capacity; ++i)
        {
            ref var slot = ref _slots[i];

            if (slot.IsOccupied)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    slot.Value = default!;

                slot.Version += 1;
            }
            slot.NextFree = i + 1;
        }
        _slots[^1].NextFree = SENTINEL;

        _nextFree = 0;
        Count = 0;
    }

    /// <inheritdoc cref="IGenIdStorage{K, T}.Resize(int)"/>
    /// <exception cref="InvalidOperationException">Thrown if the storage is occupied.</exception>
    public void Resize(int capacity)
    {
        if (Count != 0)
            throw new InvalidOperationException("attempted to resize an occupied GenIdStorage");

        if (capacity == 0)
        {
            _slots = [];
            _nextFree = SENTINEL;
            return;
        }

        Array.Resize(ref _slots, capacity);

        for (var i = 0; i < capacity - 1; ++i)
        {
            ref var _slot = ref _slots[i];
            _slot.NextFree = i + 1;
        }
        _slots[^1].NextFree = SENTINEL;
        _nextFree = 0;
    }

    /// <inheritdoc cref="IGenIdStorage{K, T}.TryRemove(in K)"/>
    public bool TryRemove(in Key key, [MaybeNullWhen(false)] out T value)
    {
        if (key.Index < 0 || key.Index >= _slots.Length)
        {
            value = default;
            return false;
        }

        ref var slot = ref _slots[key.Index];

        if (slot.Version != key.Version || !slot.IsOccupied)
        {
            value = default;
            return false;
        }

        value = slot.Value;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            slot.Value = default!;

        slot.NextFree = _nextFree;
        slot.Version += 1;

        _nextFree = key.Index;
        Count -= 1;
        return true;
    }

    /// <inheritdoc cref="IGenIdStorage{K, T}.TryGetRef(in K, out bool)"/>
    public ref T TryGetRef(in Key key, out bool success)
    {
        if (key.Index < 0 || key.Index > _slots.Length)
        {
            success = false;
            return ref Unsafe.NullRef<T>();
        }

        ref var slot = ref _slots[key.Index];

        if (slot.Version != key.Version || !slot.IsOccupied)
        {
            success = false;
            return ref Unsafe.NullRef<T>();
        }

        success = true;
        return ref slot.Value;
    }

#endregion IGenIdStorage<T> impls

#region IEnumerator<T> impls

    /// <summary>
    /// A collection representing all of the keys stored in this collection.
    /// </summary>
    /// <param name="inner"></param>
    public struct KeysCollection(GenIdStorage<T> inner) : IReadOnlyCollection<Key>
    {
        private GenIdStorage<T> _inner = inner;
        public readonly int Count => _inner.Count;

        public KeyEnumerator GetEnumerator()
        {
            return new KeyEnumerator(_inner);
        }

        IEnumerator<Key> IEnumerable<Key>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// A collection representing all of the keys stored in this collection.
    /// </summary>
    /// <param name="inner"></param>
    public struct ValuesCollection(GenIdStorage<T> inner) : IReadOnlyCollection<T>
    {
        private GenIdStorage<T> _inner = inner;
        public readonly int Count => _inner.Count;

        public ValuesEnumerator GetEnumerator()
        {
            return new ValuesEnumerator(_inner);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// An enumerator over the valid keys indexing values in a <see cref="GenIdStorage{T}"/>.
    /// </summary>
    /// <param name="inner"></param>
    public struct KeyEnumerator(GenIdStorage<T> from) : IEnumerator<Key>
    {
        private KeyValuePairsEnumerator _inner = new(from);
        public readonly Key Current => _inner.Current.Key;
        readonly object IEnumerator.Current => _inner.Current.Key;
        public bool MoveNext() => _inner.MoveNext();
        public void Reset() => _inner.Reset();
        public void Dispose() => _inner.Dispose();

        public static implicit operator KeyEnumerator(KeyValuePairsEnumerator from) => new() { _inner = from };
    }

    /// <summary>
    /// An enumerator over the values in a <see cref="GenIdStorage{T}"/>.
    /// </summary>
    public struct ValuesEnumerator(GenIdStorage<T> from) : IEnumerator<T>
    {
        private KeyValuePairsEnumerator _inner = new(from);
        public readonly T Current => _inner.Current.Value;
        readonly object IEnumerator.Current => _inner.Current.Value!;
        public bool MoveNext() => _inner.MoveNext();
        public void Reset() => _inner.Reset();
        public void Dispose() => _inner.Dispose();

        public static implicit operator ValuesEnumerator(KeyValuePairsEnumerator from) => new() { _inner = from };
    }

    /// <summary>
    /// An enumerator over all of the valid key:value pairs in a given storage.
    /// </summary>
    public struct KeyValuePairsEnumerator(GenIdStorage<T> from) : IEnumerator<KeyValueRef<Key, T>>
    {
        internal Slot[] _slots = from._slots;
        internal int _index = -1;

        public readonly KeyValueRef<Key, T> Current
        {
            get
            {
                ref var slot = ref _slots[_index];
                return new(new Key(_index, slot.Version), ref slot.Value);
            }
        }

        readonly object IEnumerator.Current => (KeyValuePair<Key, T>) Current;

        public bool MoveNext()
        {
            for (; _index < _slots.Length; ++_index)
            {
                ref var slot = ref _slots[_index];
                if (slot.IsOccupied)
                    return true;
            }

            return false;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose() { }
    }

    /// <inheritdoc cref="IGenIdStorage{K, T}.Keys"/>
    public KeysCollection Keys => new(this);
    IReadOnlyCollection<Key> IGenIdStorage<Key, T>.Keys => Keys;

    /// <inheritdoc cref="IGenIdStorage{K, T}.Values"/>
    public ValuesCollection Values => new(this);
    IReadOnlyCollection<T> IGenIdStorage<Key, T>.Values => Values;

    /// <inheritdoc cref="IEnumerable{KeyValueRef{Key, T}}.GetEnumerator()"/>
    public KeyValuePairsEnumerator GetEnumerator()
    {
        return new(this);
    }

    IEnumerator<KeyValueRef<Key, T>> IEnumerable<KeyValueRef<Key, T>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#endregion IEnumerator<T> impls

    /// <summary>
    /// ~Doubles the capacity of the collection.
    /// </summary>
    private void ReAllocate()
    {
        var oldCapacity = _slots.Length;
        var newCapacity = Math.Max(oldCapacity, 2) * 2;
        ReAllocateTo(newCapacity);
    }

    /// <summary>
    /// Attempts to set the capacity of the collection.
    /// Will fail if it attempts to shrink the collection.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    /// <param name="newCapacity">The number of slots the collection should have after the reallocation.</param>
    private void ReAllocateTo(int newCapacity)
    {
        var oldCapacity = _slots.Length;

        if (newCapacity < oldCapacity)
            throw new InvalidOperationException("cannot shrink GenIdStorage");

        Array.Resize(ref _slots, newCapacity);

        for (var i = oldCapacity; i < newCapacity - 1; ++i)
        {
            ref var slot = ref _slots[i];
            slot.Value = default!;
            slot.Version = 0;
            slot.NextFree = i + 1;
        }
        _slots[^1].NextFree = _nextFree;
        _nextFree = oldCapacity;
    }
}
