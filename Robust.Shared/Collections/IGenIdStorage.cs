using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Collections;

/// <summary>
/// An API for collections implementing "generational id storage".
/// This is essentially a dictionary that generates keys on-the-fly for inserted values.
/// </summary>
/// <typeparam name="K">The type of key used to index values stored in this collection.</typeparam>
/// <typeparam name="T">The type of values that can be stored in this collection.</typeparam>
public interface IGenIdStorage<K, T> : IEnumerable<KeyValueRef<K, T>> {
    /// <summary>
    /// The number of values currently stored in this collection.
    /// </summary>
    public abstract int Count { get; }

    /// <summary>
    /// The number of values memory is currently allocated for in this collection.
    /// </summary>
    public abstract int Capacity { get; }

    /// <summary>
    /// A collection enumerating all of the valid keys for this collection.
    /// </summary>
    public abstract IReadOnlyCollection<K> Keys { get; }

    /// <summary>
    /// A collection enumerating all of the values stored in this collection.
    /// </summary>
    public abstract IReadOnlyCollection<T> Values { get; }

    /// <summary>
    /// Creates a new storage container from a set of unique key/value pairs
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    /// <param name="pairs"></param>
    /// <returns></returns>
    public abstract static IGenIdStorage<K, T> FromEnumerable(IEnumerable<KeyValuePair<K, T>> pairs);

    /// <summary>
    /// Creates a new storage container from a set of unique key/value pairs
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    /// <param name="pairs"></param>
    /// <returns></returns>
    public abstract static IGenIdStorage<K, T> FromEnumerable(IEnumerable<(K, T)> pairs);

    /// <summary>
    /// Allocates storage for a value in this collection.
    /// </summary>
    /// <remarks>Backwards compat with old Pow3r API</remarks>
    /// <param name="key">A key indexing the newly allocated slot.</param>
    /// <returns>A reference to the allocated slot.</returns>
    public abstract ref T Allocate(out K key);

    /// <summary>
    /// Frees the value stored in a given slot and invalidates its associated key.
    /// </summary>
    /// <remarks>Backwards compat with old Pow3r API</remarks>
    /// <param name="key">A key indexing a currently allocated slot in this storage.</param>
    public abstract void Free(in K key);

    /// <summary>
    /// Fetches a reference to a value contained by this storage.
    /// Will throw an exception if the specified value does not exist.
    /// </summary>
    /// <remarks>Backwards compat with old Pow3r API</remarks>
    /// <param name="key">A key indexing a value contained by this storage.</param>
    /// <exception cref="KeyNotFoundException">Thrown if the given <paramref name="key"/> did not infact, index a value in this storage.</exception>
    /// <returns>A reference to the value in this storage which is indexed by the given <paramref name="key"/>.</returns>
    public abstract ref T this[in K key] { get; }

    /// <summary>
    /// Checks whether this storage contains a value associated with a given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">A key which may or may not index a value contained by this storage.</param>
    /// <returns>True if the storage contains a value indexed by the given <paramref name="key"/> or false if no such value exists.</returns>
    public abstract bool ContainsKey(in K key);

    /// <summary>
    /// Discards all values in this storage.
    /// </summary>
    public abstract void Clear();

    /// <summary>
    /// Grows or shrinks the amount of memory currently allocated for this collection.
    /// Only a valid operation on empty collections.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    /// <param name="capacity">The number of values we should allocate space for.</param>
    public abstract void Resize(int capacity);

    // Safe API:
    /// <summary>
    /// Allocates storage space for a value and immediately stores one.
    /// </summary>
    /// <param name="value">The value to insert into this storage.</param>
    /// <returns>The key indexing the inserted value.</returns>
    public abstract K Insert(in T value);

    /// <summary>
    /// Removes the value indexed by the given <paramref name="key"/> from this storage, if one exists.
    /// Differs from <see cref="Free"/> in that this will not throw an exception if the value doesn't exist.
    /// </summary>
    /// <param name="key">A key which may, or may not, index a stored value.</param>
    /// <param name="value">Returns the value removed from the storage, or a default value if no stored value was associated with the <paramref name="key"/>.</param>
    /// <returns>True if a value indexed by <paramref name="key"/> was located and removed, false if no such value existed otherwise.</returns>
    public abstract bool TryRemove(in K key, [MaybeNullWhen(false)] out T value);

    /// <inheritdoc cref="TryRemove(in K, out T)"/>
    public abstract bool TryRemove(in K key);

    /// <summary>
    /// Attempts to fetch a reference to a value contained by this storage.
    /// Differs from <see cref="this[in K]"/> in that this will not throw an exception if the value doesn't exist.
    /// </summary>
    /// <param name="key">A key which may or may not index a value in this storage.</param>
    /// <param name="success">Set to true if the given <paramref name="key"/> indexed a value or false if it didn't.</param>
    /// <returns>A reference to the value in this storage which is indexed by the given <paramref name="key"/> or a null ref if one did not exist.</returns>
    public abstract ref T TryGetRef(in K key, out bool success);

    /// <summary>
    /// Attempts to fetch a value contained by this storage.
    /// </summary>
    /// <param name="key">A key which may or may not index a value contained by this storage.</param>
    /// <param name="value">Set to the value indexed by the given <paramref name="key"/> if one exists, or a default value of it doesn't.</param>
    /// <returns>True if a value indexed by the given <paramref name="key"/> was successfully retrieved, or false if it wasn't.</returns>
    public abstract bool TryGet(in K key, [MaybeNullWhen(false)] out T value);
}
