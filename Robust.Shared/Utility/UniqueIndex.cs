using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Robust.Shared.Utility
{
    /// <summary>
    /// An immutable dictionary of mutable sets for use as an index of unique values related to another collection.
    /// Imitates the behavior of a read-focused index in a RDBMS.
    /// Use when the index's keys don't change that rapidly or when fast lookup is preferred over creation time.
    /// </summary>
    /// <typeparam name="TKey">The type of key.</typeparam>
    /// <typeparam name="TValue">The type of value.</typeparam>
    [PublicAPI]
    public struct UniqueIndex<TKey, TValue> : IEnumerable<KeyValuePair<TKey, ISet<TValue>>>
    {

        private ImmutableDictionary<TKey, ISet<TValue>> _index;

        /// <summary>
        /// The count of keys (and thus sets) in this index.
        /// </summary>
        public int KeyCount => _index.Count;

        /// <summary>
        /// Adds a value.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <param name="value">A value to be added.</param>
        /// <returns><c>true</c> upon success, otherwise <c>false</c>.</returns>
        public bool Add(TKey key, TValue value)
        {
            _index ??= ImmutableDictionary<TKey, ISet<TValue>>.Empty;

            if (_index.TryGetValue(key, out var set))
            {
                return set.Add(value);
            }

            _index = _index.Add(key, new HashSet<TValue> {value});
            return true;
        }

        /// <summary>
        /// Adds a collection of values to a set.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <param name="values">A collection of values.</param>
        /// <returns>The count of values that were added to the set.</returns>
        public int AddRange(TKey key, IEnumerable<TValue> values)
        {
            _index ??= ImmutableDictionary<TKey, ISet<TValue>>.Empty;

            if (_index.TryGetValue(key, out var set))
            {
                var c = set.Count;

                set.UnionWith(values);

                return set.Count - c;
            }

            _index = _index.Add(key, set = new HashSet<TValue>(values));

            return set.Count;
        }

        /// <summary>
        /// Removes a set.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <returns><c>true</c> upon success, otherwise <c>false</c>.</returns>
        public bool Remove(TKey key)
        {
            if (_index == null)
            {
                _index = ImmutableDictionary<TKey, ISet<TValue>>.Empty;
                return false;
            }

            var newIndex = _index.SetItem(key, new HashSet<TValue>());

            if (_index != newIndex)
            {
                return false;
            }

            _index = newIndex;
            return true;
        }

        /// <summary>
        /// Removes a value.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <param name="value">A value to be removed.</param>
        /// <returns><c>true</c> upon success, otherwise <c>false</c>.</returns>
        public bool Remove(TKey key, TValue value)
        {
            // ReSharper disable once InvertIf
            if (_index == null)
            {
                _index = ImmutableDictionary<TKey, ISet<TValue>>.Empty;
                return false;
            }

            return _index.TryGetValue(key, out var set)
                && set.Remove(value);
        }

        /// <summary>
        /// Removes a collection of values from a set.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <param name="values">A collection of values.</param>
        /// <returns>The count of values that were removed from the set.</returns>
        public int RemoveRange(TKey key, IEnumerable<TValue> values)
        {
            if (_index == null)
            {
                _index = ImmutableDictionary<TKey, ISet<TValue>>.Empty;
                return 0;
            }

            if (!_index.TryGetValue(key, out var set))
            {
                return 0;
            }

            var c = set.Count;

            set.ExceptWith(set);

            return c - set.Count;
        }

        /// <summary>
        /// Replaces a old value with a new value.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <param name="oldValue">A value to be replaced.</param>
        /// <param name="newValue">A value to replace with.</param>
        /// <returns><c>true</c> upon success, otherwise <c>false</c>.</returns>
        public bool Replace(TKey key, TValue oldValue, TValue newValue)
        {
            if (_index == null)
            {
                _index = ImmutableDictionary<TKey, ISet<TValue>>.Empty;
                return false;
            }

            if (!_index.TryGetValue(key, out var set))
            {
                return false;
            }

            return set.Remove(oldValue)
                && set.Add(newValue);
        }

        /// <summary>
        /// Creates ensures an empty mutable set for a given key.
        /// </summary>
        /// <param name="key">A given key.</param>
        public void Touch(TKey key)
        {
            _index ??= ImmutableDictionary<TKey, ISet<TValue>>.Empty;

            if (_index.ContainsKey(key)) return;

            _index = _index.Add(key, new HashSet<TValue>());
        }

        /// <summary>
        /// Initializes the index from a collection of keys.
        /// </summary>
        /// <param name="keys">A collection of keys.</param>
        /// <exception cref="InvalidOperationException">Already initialized.</exception>
        public void Initialize(IEnumerable<TKey> keys)
            => Initialize(keys.Select(k => new KeyValuePair<TKey, ISet<TValue>>(k, new HashSet<TValue>())));

        /// <summary>
        /// Initializes the index from an equivalent collection.
        /// </summary>
        /// <param name="index">An equivalent collection.</param>
        /// <exception cref="InvalidOperationException">Already initialized.</exception>
        public void Initialize(IEnumerable<KeyValuePair<TKey, ISet<TValue>>> index)
        {
            if (_index != null) throw new InvalidOperationException("Already initialized.");

            _index = ImmutableDictionary.CreateRange(index);
        }

        /// <summary>
        /// Accesses the mutable set for a given key.
        /// </summary>
        /// <param name="key">A given key.</param>
        public ISet<TValue> this[TKey key]
        {
            get
            {
                _index ??= ImmutableDictionary<TKey, ISet<TValue>>.Empty;

                if (_index.TryGetValue(key, out var set)) return set;

                _index = _index.Add(key, set = new HashSet<TValue>());

                return set;
            }
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<TKey, ISet<TValue>>> GetEnumerator() => _index.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

}
