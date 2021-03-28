#nullable enable
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
    /// A mutable dictionary of mutable and immutable sets for use as an index of unique values related to another collection.
    /// Imitates the behavior of a write-focused index in a RDBMS.
    /// </summary>
    /// <remarks>
    /// Use when the index's keys change more rapidly or when insertion and removal of keys is preferred over read time.
    /// It is intended to explicitly construct this index before use.
    /// Do not refer to a <see cref="UniqueIndexHkm{TKey,TValue}"/> by it's interface.
    /// See <see cref="IUniqueIndex{TKey,TValue}"/> for details.
    /// </remarks>
    /// <typeparam name="TKey">The type of key.</typeparam>
    /// <typeparam name="TValue">The type of value.</typeparam>
    /// <seealso cref="UniqueIndexExtensions"/>
    /// <seealso cref="IUniqueIndex{TKey,TValue}" />
    [PublicAPI]
    public struct UniqueIndexHkm<TKey, TValue> : IUniqueIndex<TKey, TValue> where TKey : notnull
    {

        [NotNull]
        private readonly Dictionary<TKey, ISet<TValue>> _index;

        public UniqueIndexHkm(int capacity)
            => _index = new Dictionary<TKey, ISet<TValue>>(capacity);

        /// <inheritdoc />
        public int KeyCount => _index.Count;

        private void InitializedCheck()
        {
            if (_index == null) throw new NotSupportedException("UniqueIndexHkm instances must use the non-default constructor.");
        }

        /// <inheritdoc />
        public bool Add(TKey key, TValue value)
        {
            InitializedCheck();

            if (_index.TryGetValue(key, out var set))
            {
                return set.Add(value);
            }

            _index.Add(key, new HashSet<TValue> {value});
            return true;
        }

        /// <inheritdoc />
        public int AddRange(TKey key, IEnumerable<TValue> values)
        {
            InitializedCheck();

            if (_index.TryGetValue(key, out var set))
            {
                var c = set.Count;

                set.UnionWith(values);

                return set.Count - c;
            }

            _index.Add(key, set = new HashSet<TValue>(values));

            return set.Count;
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            InitializedCheck();

            var c = _index.Count;

            if (c == 0) return false;

            _index[key] = new HashSet<TValue>();

            return c > _index.Count;
        }

        /// <inheritdoc />
        public bool Remove(TKey key, TValue value)
        {
            InitializedCheck();

            if (_index.Count == 0) return false;

            return _index.TryGetValue(key, out var set)
                && set.Remove(value);
        }

        /// <inheritdoc />
        public int RemoveRange(TKey key, IEnumerable<TValue> values)
        {
            InitializedCheck();

            if (_index.Count == 0) return 0;

            if (!_index.TryGetValue(key, out var set)) return 0;

            var c = set.Count;

            set.ExceptWith(set);

            return c - set.Count;
        }

        /// <inheritdoc />
        public bool Replace(TKey key, TValue oldValue, TValue newValue)
        {
            InitializedCheck();

            if (_index.Count == 0) return false;

            if (!_index.TryGetValue(key, out var set))
            {
                return false;
            }

            return set.Remove(oldValue)
                && set.Add(newValue);
        }

        /// <inheritdoc />
        public void Touch(TKey key)
        {
            InitializedCheck();

            if (_index.ContainsKey(key)) return;

            _index.Add(key, new HashSet<TValue>());
        }

        /// <inheritdoc />
        public bool Freeze(TKey key)
        {
            InitializedCheck();

            if (!_index.TryGetValue(key, out var set)
                || set is ImmutableHashSet<TValue>)
            {
                return false;
            }

            _index[key] = ImmutableHashSet.CreateRange(set);
            return true;
        }

        /// <inheritdoc />
        public void Initialize(IEnumerable<TKey> keys)
            => Initialize(keys.Select(k => new KeyValuePair<TKey, ISet<TValue>>(k, new HashSet<TValue>())));

        /// <inheritdoc />
        public void Initialize(IEnumerable<KeyValuePair<TKey, ISet<TValue>>> index)
        {
            InitializedCheck();

            if (_index.Count != 0) throw new InvalidOperationException("Already initialized.");

            foreach (var (key, set) in index)
            {
                _index.Add(key, set);
            }
        }

        /// <inheritdoc />
        public ISet<TValue> this[TKey key]
        {
            get
            {
                InitializedCheck();

                if (_index.TryGetValue(key, out var set)) return set;

                _index.Add(key, set = new HashSet<TValue>());

                return set;
            }
        }

        /// <inheritdoc cref="IEnumerable{T}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<TKey, ISet<TValue>>> GetEnumerator()
        {
            InitializedCheck();

            return _index.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Clear()
        {
            InitializedCheck();

            _index.Clear();
        }

    }

}
