#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Utility.Internal;

namespace Robust.Shared.Utility
{

    /// <summary>
    /// An immutable dictionary of mutable and immutable sets for use as an index of unique values related to another collection.
    /// Imitates the behavior of a read-focused index in a RDBMS.
    /// </summary>
    /// <remarks>
    /// Use when the index's keys don't change that rapidly or when fast lookup is preferred over creation time.
    /// It is not intended to explicitly construct this index before use.
    /// Do not refer to a <see cref="UniqueIndex{TKey,TValue}"/> by it's interface.
    /// See <see cref="IUniqueIndex{TKey,TValue}"/> for details.
    /// </remarks>
    /// <typeparam name="TKey">The type of key.</typeparam>
    /// <typeparam name="TValue">The type of value.</typeparam>
    /// <seealso cref="UniqueIndexExtensions"/>
    /// <seealso cref="IUniqueIndex{TKey,TValue}" />
    [PublicAPI]
    public struct UniqueIndex<TKey, TValue> : IUniqueIndex<TKey, TValue> where TKey : notnull
    {

        private ImmutableDictionary<TKey, ISet<TValue>>? _index;

        /// <inheritdoc />
        public int KeyCount => _index?.Count ?? 0;

        /// <inheritdoc />
        public bool Add(TKey key, TValue value)
        {
            ISet<TValue>? set;

            if (_index is null)
            {
                set = new HashSet<TValue> {value};
                _index = ImmutableDictionary.CreateRange(new[] {new KeyValuePair<TKey, ISet<TValue>>(key, set)});
                return true;
            }

            if (_index.TryGetValue(key, out set))
            {
                return set.Add(value);
            }

            _index = _index.Add(key, new HashSet<TValue> {value});
            return true;
        }

        /// <inheritdoc />
        public int AddRange(TKey key, IEnumerable<TValue> values)
        {
            ISet<TValue>? set;

            if (_index is null)
            {
                set = new HashSet<TValue>(values);
                _index = ImmutableDictionary.CreateRange(new[] {new KeyValuePair<TKey, ISet<TValue>>(key, set)});
                return set.Count;
            }

            if (_index.TryGetValue(key, out set))
            {
                var c = set.Count;

                set.UnionWith(values);

                return set.Count - c;
            }

            _index = _index.Add(key, set = new HashSet<TValue>(values));

            return set.Count;
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            if (_index == null)
            {
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

        /// <inheritdoc />
        public bool Remove(TKey key, TValue value)
        {
            // ReSharper disable once InvertIf
            if (_index == null)
            {
                return false;
            }

            return _index.TryGetValue(key, out var set)
                && set.Remove(value);
        }

        /// <inheritdoc />
        public int RemoveRange(TKey key, IEnumerable<TValue> values)
        {
            if (_index == null)
            {
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

        /// <inheritdoc />
        public bool Replace(TKey key, TValue oldValue, TValue newValue)
        {
            if (_index == null)
            {
                return false;
            }

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
            _index ??= ImmutableDictionary<TKey, ISet<TValue>>.Empty;

            if (_index.ContainsKey(key)) return;

            _index = _index.Add(key, new HashSet<TValue>());
        }

        /// <inheritdoc />
        public bool Freeze(TKey key)
        {
            if (_index is null)
            {
                return false;
            }

            if (!_index.TryGetValue(key, out var set)
                || set is ImmutableHashSet<TValue>)
            {
                return false;
            }

            _index = _index.SetItem(key, ImmutableHashSet.CreateRange(set));
            return true;
        }

        /// <inheritdoc />
        public void Initialize(IEnumerable<TKey> keys)
            => Initialize(keys.Select(k => new KeyValuePair<TKey, ISet<TValue>>(k, new HashSet<TValue>())));

        /// <inheritdoc />
        public void Initialize(IEnumerable<KeyValuePair<TKey, ISet<TValue>>> index)
        {
            if (_index != null) throw new InvalidOperationException("Already initialized.");

            _index = ImmutableDictionary.CreateRange(index);
        }

        public ISet<TValue> this[TKey key]
        {
            get
            {
                ISet<TValue>? set;

                if (_index is null)
                {
                    _index = ImmutableDictionary<TKey, ISet<TValue>>.Empty;
                }
                else
                {
                    if (_index.TryGetValue(key, out set))
                    {
                        return set;
                    }
                }

                _index = _index.Add(key, set = new HashSet<TValue>());

                return set;
            }
        }

        /// <inheritdoc cref="IEnumerable{T}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<TKey, ISet<TValue>>> GetEnumerator()
        {
            if (_index != null)
            {
                return _index.GetEnumerator();
            }

            return Enumerable.Empty<KeyValuePair<TKey, ISet<TValue>>>().GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

}
